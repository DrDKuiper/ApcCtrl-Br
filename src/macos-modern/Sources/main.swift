// (moved into Settings) kCycleCount, cycleCount
// apcctrl macOS modern agent prototype
// GPLv2 - see COPYING in project root
// Minimal menubar app in Swift using AppKit

import Cocoa
import UserNotifications
import Foundation
import SwiftUI
import ServiceManagement
// Charts is available on macOS 13+
#if canImport(Charts)
import Charts
#endif

// Simple NIS client to talk to apcctrl daemon
final class NisClient {
    let host: String
    let port: Int
    init(host: String = "127.0.0.1", port: Int = 3551) {
        self.host = host; self.port = port
    }

    

    enum UpsState: String { case commlost, onbatt, charging, online }

    func fetchStatus(timeout: TimeInterval = 3.0) -> (UpsState, [String:String]) {
        var dict: [String:String] = [:]
        var state: UpsState = .commlost

        // NIS: Connect and send "status\n"
        let semaphore = DispatchSemaphore(value: 0)
        DispatchQueue.global().async {
            defer { semaphore.signal() }
            if let (input, output) = self.openStream() {
                let cmd = "status\n"
                if let data = cmd.data(using: .ascii) {
                    data.withUnsafeBytes { ptr in
                        guard let base = ptr.baseAddress?.assumingMemoryBound(to: UInt8.self) else { return }
                        output.write(base, maxLength: data.count)
                    }
                }
                output.close() // server pushes status then closes

                let bufferSize = 4096
                let buffer = UnsafeMutablePointer<UInt8>.allocate(capacity: bufferSize)
                defer { buffer.deallocate() }
                var raw = Data()
                while true {
                    let read = input.read(buffer, maxLength: bufferSize)
                    if read <= 0 { break } // 0 = EOF, -1 = error
                    raw.append(buffer, count: read)
                }
                input.close()

                if let text = String(data: raw, encoding: .ascii) {
                    dict = self.parseKeyValueText(text)
                }
            }

            // Fallback: if NIS didn't return anything usable, try running apcaccess locally
            if dict.isEmpty || dict["STATUS"] == nil {
                if let text = self.runApcaccess() {
                    dict = self.parseKeyValueText(text)
                }
            }

            // Determine state heuristics using fields (NIS or apcaccess)
            if let status = dict["STATUS"] {
                if status.contains("COMMLOST") { state = .commlost }
                else if status.contains("ONBATT") { state = .onbatt }
                else if status.contains("ONLINE") { state = .online }
                else { state = .charging }
            } else {
                // If we still have no status but have charge < 100, call charging
                if dict["BCHARGE"].flatMap({ Double($0.split(separator: " ").first ?? "0") }) ?? 100 < 100 { state = .charging }
            }
        }
        _ = semaphore.wait(timeout: .now() + timeout)
        return (state, dict)
    }

    func fetchEvents(timeout: TimeInterval = 3.0) -> [String] {
        var events: [String] = []
        let semaphore = DispatchSemaphore(value: 0)
        DispatchQueue.global().async {
            defer { semaphore.signal() }
            guard let (input, output) = self.openStream() else { return }
            let cmd = "events\n"
            if let data = cmd.data(using: .ascii) {
                data.withUnsafeBytes { ptr in
                    guard let base = ptr.baseAddress?.assumingMemoryBound(to: UInt8.self) else { return }
                    output.write(base, maxLength: data.count)
                }
            }
            output.close()

            let bufferSize = 4096
            let buffer = UnsafeMutablePointer<UInt8>.allocate(capacity: bufferSize)
            defer { buffer.deallocate() }
            var data = Data()
            while true {
                let read = input.read(buffer, maxLength: bufferSize)
                if read <= 0 { break }
                data.append(buffer, count: read)
            }
            input.close()

            if let text = String(data: data, encoding: .ascii) {
                // Cada linha já é um evento textual do NIS
                events = text.split(separator: "\n").map { String($0) }
            }
        }
        _ = semaphore.wait(timeout: .now() + timeout)
        return events
    }

    private func openStream() -> (InputStream, OutputStream)? {
        var inS: InputStream?; var outS: OutputStream?
        Stream.getStreamsToHost(withName: host, port: port, inputStream: &inS, outputStream: &outS)
        guard let i = inS, let o = outS else { return nil }
        i.open(); o.open(); return (i,o)
    }

    // Parse KEY : VALUE lines common to NIS and apcaccess outputs
    private func parseKeyValueText(_ text: String) -> [String: String] {
        var d: [String: String] = [:]
        for line in text.split(separator: "\n") {
            let parts = line.split(separator: ":", maxSplits: 1)
            if parts.count == 2 {
                let key = parts[0].trimmingCharacters(in: .whitespaces)
                let value = parts[1].trimmingCharacters(in: .whitespaces)
                d[key] = value
            }
        }
        return d
    }

    // Try executing apcaccess to fetch local status when NIS is unavailable
    private func runApcaccess() -> String? {
        let candidates = [
            "/opt/homebrew/sbin/apcaccess", "/opt/homebrew/bin/apcaccess",
            "/usr/local/sbin/apcaccess", "/usr/local/bin/apcaccess",
            "/usr/sbin/apcaccess", "/usr/bin/apcaccess", "apcaccess"
        ]
        for path in candidates {
            let proc = Process()
            if path == "apcaccess" {
                proc.launchPath = "/usr/bin/env"
                proc.arguments = ["apcaccess"]
            } else {
                if !FileManager.default.isExecutableFile(atPath: path) { continue }
                proc.launchPath = path
            }
            let pipe = Pipe()
            proc.standardOutput = pipe
            proc.standardError = Pipe()
            do {
                try proc.run()
            } catch {
                continue
            }
            let data = try? pipe.fileHandleForReading.readToEnd()
            proc.waitUntilExit()
            if let data = data, let text = String(data: data, encoding: .ascii), !text.isEmpty {
                return text
            }
        }
        return nil
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate, UNUserNotificationCenterDelegate {
    let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
    var client: NisClient!
    var timer: Timer?
    var lastEventLine: String = ""
    var lastStatusText: String = ""
    var eventsCache: [String] = []
    var eventsWindow: EventsWindowController?
    var selfTestsWindow: SelfTestsWindowController?
    var graphsWindow: GraphsWindowController?
    var statusWindow: NSWindow?
    // Track last overall state to detect ONBATT transitions
    var lastState: NisClient.UpsState = .commlost
    // Track alert states to avoid spamming repeated voltage/frequency notifications
    var lastVoltageAlerted: Bool = false
    var lastFrequencyAlerted: Bool = false
    // Track on-battery timing
    var onBatteryStart: Date? = nil
    var lastOnBatteryDuration: TimeInterval = 0
    // Capacity estimation state (captured at start of ONBATT)
    var onBattStartBcharge: Double? = nil
    var onBattStartLoadPct: Double? = nil
    var onBattStartBattV: Double? = nil
    // Agendamento de log diário
    var dailyLogTimer: Timer?
    var lastDailyLogDate: Date? = nil
    var dailyLogHour: Int = 8 // Agora configurável pelo usuário
    // Cache last known UPS name to avoid blocking lookups in notifications
    var lastUpsName: String = "UPS"
    // Metrics store for charts
    let metrics = MetricsStore()

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let button = statusItem.button { button.image = makeIcon(system: "exclamationmark.circle") }
        // Carregar configurações persistidas
        Settings.shared.load()
        client = NisClient(host: Settings.shared.host, port: Settings.shared.port)
        // Restaurar temporização de bateria persistida
        if Settings.shared.onBattStartEpoch > 0 {
            onBatteryStart = Date(timeIntervalSince1970: Settings.shared.onBattStartEpoch)
        }
        lastOnBatteryDuration = Settings.shared.lastOnBattSeconds
        constructMenu()
        // Carregar horário do log diário da configuração
        if Settings.shared.dailyLogHour != 0 {
            dailyLogHour = Settings.shared.dailyLogHour
        }
        // Solicitar permissão para notificações (somente quando rodando dentro de um .app bundle)
        if canUseUserNotifications() {
            let center = UNUserNotificationCenter.current()
            center.delegate = self
            center.requestAuthorization(options: [.alert, .sound, .badge]) { granted, error in
                print("[Notifications] Permission granted: \(granted), error: \(String(describing: error))")
                if granted {
                    print("[Notifications] Alerts enabled. You can now receive notifications.")
                } else {
                    print("[Notifications] Permission denied. Enable in System Settings → Notifications → ApcCtrl")
                }
            }
        } else {
            print("[Notifications] Not running as .app bundle, notifications disabled")
        }
        poll()
        startTimer()
        startDailyLogTimer()
    }

    // Inicia (ou reinicia) o timer de polling principal
    func startTimer() {
        timer?.invalidate()
        let interval = max(2.0, Double(Settings.shared.refreshSeconds))
        timer = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { [weak self] _ in
            self?.poll()
        }
        if let t = timer { RunLoop.main.add(t, forMode: .common) }
    }
    // Constroi/Reconstroi o menu principal
    func constructMenu() {
        let menu = NSMenu()
        if #available(macOS 11.0, *) {
            menu.addItem(NSMenuItem(title: "Status", action: #selector(showStatus), keyEquivalent: "s"))
        } else {
            menu.addItem(NSMenuItem(title: "Status", action: #selector(showStatus), keyEquivalent: ""))
        }
        menu.addItem(NSMenuItem(title: "Eventos", action: #selector(showEvents), keyEquivalent: "e"))
        menu.addItem(NSMenuItem(title: "Notificação de teste", action: #selector(sendTestNotification), keyEquivalent: "n"))
        menu.addItem(NSMenuItem(title: "Enviar log do status", action: #selector(sendStatusLog), keyEquivalent: "g"))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Configuração", action: #selector(showConfig), keyEquivalent: "c"))
        menu.addItem(NSMenuItem(title: "Limpar eventos", action: #selector(clearEvents), keyEquivalent: "l"))
        menu.addItem(NSMenuItem(title: "Autotestes", action: #selector(showSelfTests), keyEquivalent: "t"))
    menu.addItem(NSMenuItem(title: "Gráficos", action: #selector(showGraphs), keyEquivalent: "h"))
        menu.addItem(NSMenuItem.separator())
        // Preferências de Notificações (atalho para sistema)
        menu.addItem(NSMenuItem(title: "Abrir Preferências de Notificações", action: #selector(openNotificationSettings), keyEquivalent: ""))
        // Auto-start toggle
        let autoStartItem = NSMenuItem(title: "Iniciar com o sistema", action: #selector(toggleAutoStart), keyEquivalent: "a")
        autoStartItem.state = isAutoStartEnabled() ? .on : .off
        menu.addItem(autoStartItem)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Sair", action: #selector(quit), keyEquivalent: "q"))
        statusItem.menu = menu
    }

    // Monta e envia o log consolidado dos eventos do dia (inclui ciclos e capacidade)
    func sendDailyEventsLog() {
        let calendar = Calendar.current
        let today = calendar.startOfDay(for: Date())
        // Filtra eventos do dia (espera timestamp ISO curto no início: yyyy-MM-dd ...)
        let eventosHoje = eventsCache.filter { line in
            guard let first = line.split(separator: " ", maxSplits: 1, omittingEmptySubsequences: true).first else { return false }
            let dstr = String(first)
            if dstr.count < 10 { return false }
            return DateFormatter.cached.date(from: String(dstr.prefix(10)))?.timeIntervalSince1970 ?? 0 >= today.timeIntervalSince1970
        }
        let name = lastUpsName
        var log = "[Log diário do nobreak]"
        log += "\nNome: \(name)"
        log += "\nData: \(DateFormatter.cached.string(from: Date()))"
        // Ciclos e capacidade
        let cycles = Settings.shared.cycleCount
        let capAh = Settings.shared.estimatedCapacityAh
        let capLine = capAh > 0 ? String(format: "%.1f Ah (%d amostras)", capAh, Settings.shared.estimatedCapacitySamples) : "--"
        log += "\nCiclos: \(cycles)"
        log += "\nCapacidade estimada: \(capLine)"
        // Eventos
        log += "\nEventos do dia:"
        if eventosHoje.isEmpty { log += "\n(nenhum evento registrado hoje)" }
        else { log += "\n- " + eventosHoje.joined(separator: "\n- ") }
        sendTelegram(body: log)
        print("[DailyLog] Log diário enviado para o Telegram")
    }
    // Inicia timer para envio automático do log diário
    func startDailyLogTimer() {
        dailyLogTimer?.invalidate()
        dailyLogTimer = Timer.scheduledTimer(withTimeInterval: 60, repeats: true) { [weak self] _ in
            self?.checkAndSendDailyLog()
        }
        RunLoop.main.add(dailyLogTimer!, forMode: .common)
    }

    // Verifica se é hora de enviar o log diário e envia se necessário
    func checkAndSendDailyLog() {
        let now = Date()
        let calendar = Calendar.current
        let hour = calendar.component(.hour, from: now)
        let minute = calendar.component(.minute, from: now)
        let today = calendar.startOfDay(for: now)
        // Só envia uma vez por dia, no horário programado (ex: 08:00)
        if hour == dailyLogHour && minute < 5 {
            if lastDailyLogDate == nil || calendar.startOfDay(for: lastDailyLogDate!) < today {
                sendDailyEventsLog()
                lastDailyLogDate = now
            }
        }
    }
    // Alterna início automático com o sistema
    @objc func toggleAutoStart(_ sender: NSMenuItem) {
        let enabled = !isAutoStartEnabled()
        setAutoStart(enabled)
        sender.state = enabled ? .on : .off
    }

    // Verifica se o app está configurado para iniciar com o sistema
    func isAutoStartEnabled() -> Bool {
        if #available(macOS 13.0, *) {
            return SMAppService.mainApp.status == .enabled
        } else {
            let bundleID = Bundle.main.bundleIdentifier ?? ""
            let jobs = (SMCopyAllJobDictionaries(kSMDomainUserLaunchd)?.takeRetainedValue() as? [[String: AnyObject]]) ?? []
            return jobs.contains { ($0["Label"] as? String) == bundleID }
        }
    }

    // Ativa/desativa início automático usando SMAppService (Ventura+) ou ServiceManagement
    func setAutoStart(_ enabled: Bool) {
        if #available(macOS 13.0, *) {
            if enabled {
                try? SMAppService.mainApp.register()
            } else {
                try? SMAppService.mainApp.unregister()
            }
        } else {
            // Para versões antigas, usar ServiceManagement
            let bundleID = Bundle.main.bundleIdentifier! as CFString
            SMLoginItemSetEnabled(bundleID, enabled)
        }
    }

    // Envia um snapshot de status com ciclos e capacidade
    @objc func sendStatusLog() {
        let (_, dict) = client.fetchStatus()
        var log = "[Resumo do Nobreak]"
        let name = dict["UPSNAME"] ?? lastUpsName
        let status = dict["STATUS"] ?? "?"
        let charge = dict["BCHARGE"] ?? "--"
        let load = dict["LOADPCT"] ?? "--"
        let linev = dict["LINEV"] ?? "--"
        let outputv = dict["OUTPUTV"] ?? "--"
        let freq = dict["LINEFREQ"] ?? "--"
        let timeleft = dict["TIMELEFT"] ?? "--"
        let cycles = Settings.shared.cycleCount
        let capAh = Settings.shared.estimatedCapacityAh
        let capLine = capAh > 0 ? String(format: "%.1f Ah (%d amostras)", capAh, Settings.shared.estimatedCapacitySamples) : "--"
        log += "\nNome: \(name)"
        log += "\nStatus: \(status)"
        log += "\nBateria: \(charge)"
        log += "\nCarga: \(load)"
        log += "\nTensão entrada: \(linev)"
        log += "\nTensão saída: \(outputv)"
        log += "\nFrequência: \(freq)"
        log += "\nTempo restante: \(timeleft)"
        log += "\nCiclos: \(cycles)"
        log += "\nCapacidade estimada: \(capLine)"
        // Últimos eventos
        let ultimos = eventsCache.suffix(5).joined(separator: "\n- ")
        if !ultimos.isEmpty { log += "\nEventos recentes:\n- \(ultimos)" }
        sendTelegram(body: log)
        simpleAlert(title: "Log enviado", message: "Resumo enviado para o Telegram.")
    }

    @objc func poll() {
        print("[Poll] Starting fetch at \(Date())")
        // Run fetch in background to avoid blocking timer
        DispatchQueue.global(qos: .userInitiated).async { [weak self] in
            guard let self = self else { return }
            let (state, dict) = self.client.fetchStatus()
            print("[Poll] Fetched status: \(dict["STATUS"] ?? "N/A"), state: \(state)")
            // Incrementa ciclo se transição para ONBATT
            if self.lastState != .onbatt && state == .onbatt {
                Settings.shared.cycleCount += 1
                Settings.shared.save()
            }
            self.lastState = state
            
            // Update UI on main thread
            DispatchQueue.main.async {
                // Append metric sample for charts
                func parseFirst(_ raw: String?) -> Double? {
                    guard let raw = raw else { return nil }
                    let first = raw.split(separator: " ").first
                    return first.flatMap { Double($0.replacingOccurrences(of: ",", with: ".")) }
                }
                let sample = MetricSample(
                    time: Date(),
                    charge: parseFirst(dict["BCHARGE"]),
                    load: parseFirst(dict["LOADPCT"]),
                    lineV: parseFirst(dict["LINEV"]),
                    freq: parseFirst(dict["LINEFREQ"]))
                self.metrics.append(sample)
                if let button = self.statusItem.button {
                    switch state {
                    case .commlost: button.image = self.makeIcon(system: "bolt.slash")
                    case .onbatt:   button.image = self.makeIcon(system: "bolt.fill")
                    case .charging: button.image = self.makeIcon(system: "battery.50")
                    case .online:   button.image = self.makeIcon(system: "checkmark.circle")
                    }
                    let statusText = dict["STATUS"] ?? "Desconhecido"
                    self.lastUpsName = (dict["UPSNAME"]?.isEmpty == false) ? dict["UPSNAME"]! : self.lastUpsName
                    // Atualiza cronômetro de bateria e tooltip com emoji
                    let isOnBatt = statusText.contains("ONBATT")
                    if isOnBatt {
                        if self.onBatteryStart == nil {
                            self.onBatteryStart = Date()
                            Settings.shared.onBattStartEpoch = self.onBatteryStart!.timeIntervalSince1970
                            Settings.shared.save()
                            // Capture start-of-cycle metrics
                            if let ch = dict["BCHARGE"], let v = ch.split(separator: " ").first, let pv = Double(v.replacingOccurrences(of: ",", with: ".")) {
                                self.onBattStartBcharge = pv
                            } else { self.onBattStartBcharge = nil }
                            if let ld = dict["LOADPCT"], let v = ld.split(separator: " ").first, let pl = Double(v.replacingOccurrences(of: ",", with: ".")) {
                                self.onBattStartLoadPct = pl
                            } else { self.onBattStartLoadPct = nil }
                            if let bv = dict["BATTV"]?.split(separator: " ").first, let p = Double(bv.replacingOccurrences(of: ",", with: ".")) {
                                self.onBattStartBattV = p
                            } else { self.onBattStartBattV = nil }
                        }
                    } else if let start = self.onBatteryStart {
                        let dur = Date().timeIntervalSince(start)
                        self.lastOnBatteryDuration = dur
                        Settings.shared.lastOnBattSeconds = dur
                        Settings.shared.onBattStartEpoch = 0
                        Settings.shared.save()
                        // Compute estimated capacity if we have start metrics and current percent
                        if let startPct = self.onBattStartBcharge {
                            var endPct: Double? = nil
                            if let ch = dict["BCHARGE"], let v = ch.split(separator: " ").first, let pv = Double(v.replacingOccurrences(of: ",", with: ".")) {
                                endPct = pv
                            }
                            let loadPct = (self.onBattStartLoadPct ?? dict["LOADPCT"].flatMap { $0.split(separator: " ").first }.flatMap { Double($0.replacingOccurrences(of: ",", with: ".")) } ) ?? 0
                            let battVolt = dict["NOMBATTV"].flatMap { Double($0.split(separator: " ").first?.replacingOccurrences(of: ",", with: ".") ?? "") } ?? self.onBattStartBattV ?? Settings.shared.batteryNominalVoltage
                            // Tentar VA (NOMAPNT) -> converter para Watts usando PF assumido se NOMPOWER não disponível
                            let nomWatts = dict["NOMPOWER"].flatMap { Double($0.split(separator: " ").first?.replacingOccurrences(of: ",", with: ".") ?? "") }
                            let nomVa = dict["NOMAPNT"].flatMap { Double($0.split(separator: " ").first?.replacingOccurrences(of: ",", with: ".") ?? "") }
                            let upsWatts = nomWatts ?? (nomVa.map { $0 * Settings.shared.assumedPowerFactor }) ?? Settings.shared.upsNominalWatts
                            let eff = 0.85
                            let pout = max(10.0, upsWatts * (loadPct/100.0))
                            let iLoad = pout / max(10.0, battVolt * eff)
                            if let end = endPct, startPct > end {
                                let deltaSOC = max(0.05, (startPct - end) / 100.0)
                                let hours = max(0.01, dur / 3600.0)
                                // Duas baterias em série: tensão dobra, Ah permanece (pack equivalente mantém Ah). Usamos battVolt já refletindo tensão total.
                                let estAh = (iLoad * hours) / deltaSOC
                                // EMA smoothing
                                let prev = Settings.shared.estimatedCapacityAh
                                let alpha = 0.3
                                let newEst = (prev <= 0) ? estAh : (alpha * estAh + (1 - alpha) * prev)
                                Settings.shared.estimatedCapacityAh = newEst
                                Settings.shared.estimatedCapacitySamples = Settings.shared.estimatedCapacitySamples + 1
                                Settings.shared.save()
                            }
                        }
                        self.onBatteryStart = nil
                        self.onBattStartBcharge = nil
                        self.onBattStartLoadPct = nil
                        self.onBattStartBattV = nil
                    }
                    let emoji = self.emojiForStatus(statusText)
                    var batteryLine = ""
                    if let start = self.onBatteryStart {
                        batteryLine = "\n🔋 Em bateria há: \(self.formatDuration(Date().timeIntervalSince(start)))"
                    } else if self.lastOnBatteryDuration > 0 {
                        batteryLine = "\n🔋 Último ciclo em bateria: \(self.formatDuration(self.lastOnBatteryDuration))"
                    }
                    button.toolTip = "UPS: \(dict["UPSNAME"] ?? "?")\nStatus: \(emoji) \(statusText)\(batteryLine)"
                }

                // Buscar eventos NIS
                let nisEvents = self.client.fetchEvents()
                let currentStatus = dict["STATUS"] ?? ""
                let previousCount = self.eventsCache.count
                if nisEvents.isEmpty {
                    // Sintetizar mudança de STATUS
                    if self.eventsCache.isEmpty && !currentStatus.isEmpty {
                        self.eventsCache.append("\(self.timestamp()) Inicializado - Status: \(currentStatus)")
                        self.lastStatusText = currentStatus
                    } else if !currentStatus.isEmpty && currentStatus != self.lastStatusText {
                        self.eventsCache.append("\(self.timestamp()) Mudança - Status: \(currentStatus)")
                        // Eventos de entrada/saída de bateria
                        let wasOnBatt = self.lastStatusText.contains("ONBATT")
                        let nowOnBatt = currentStatus.contains("ONBATT")
                        if nowOnBatt && !wasOnBatt {
                            self.eventsCache.append("\(self.timestamp()) Entrou em bateria")
                        } else if !nowOnBatt && wasOnBatt {
                            let durText = self.lastOnBatteryDuration > 0 ? self.formatDuration(self.lastOnBatteryDuration) : "--:--"
                            self.eventsCache.append("\(self.timestamp()) Saiu da bateria (duração \(durText))")
                        }
                        self.lastStatusText = currentStatus
                    }
                } else {
                    self.eventsCache = nisEvents
                    self.lastStatusText = currentStatus
                }

                // Voltage/Frequency monitoring
                let s = Settings.shared
                if s.voltageAlertsEnabled {
                    func parseFirst(_ raw: String?) -> Double? {
                        guard let raw = raw else { return nil }
                        let first = raw.split(separator: " ").first
                        return first.flatMap { Double($0.replacingOccurrences(of: ",", with: ".")) }
                    }
                    if let linev = parseFirst(dict["LINEV"]) {
                        if (linev < s.voltageLow || linev > s.voltageHigh) {
                            if !self.lastVoltageAlerted {
                                let kind = linev < s.voltageLow ? "SUBTENSÃO" : "SOBRETENSÃO"
                                self.eventsCache.append("\(self.timestamp()) Alerta \(kind) LINEV=\(String(format: "%.1f", linev))V (limites \(s.voltageLow)-\(s.voltageHigh))")
                                self.lastVoltageAlerted = true
                            }
                        } else if self.lastVoltageAlerted {
                            self.eventsCache.append("\(self.timestamp()) Tensão normalizada LINEV=\(String(format: "%.1f", linev))V")
                            self.lastVoltageAlerted = false
                        }
                    }
                    if let freq = parseFirst(dict["LINEFREQ"]) {
                        if (freq < s.frequencyLow || freq > s.frequencyHigh) {
                            if !self.lastFrequencyAlerted {
                                let kind = freq < s.frequencyLow ? "FREQ BAIXA" : "FREQ ALTA"
                                self.eventsCache.append("\(self.timestamp()) Alerta \(kind) LINEFREQ=\(String(format: "%.1f", freq))Hz (limites \(s.frequencyLow)-\(s.frequencyHigh))")
                                self.lastFrequencyAlerted = true
                            }
                        } else if self.lastFrequencyAlerted {
                            self.eventsCache.append("\(self.timestamp()) Frequência normalizada LINEFREQ=\(String(format: "%.1f", freq))Hz")
                            self.lastFrequencyAlerted = false
                        }
                    }
                }

                if self.eventsCache.count > previousCount {
                    // Enrich only the newly added events
                    for i in previousCount..<self.eventsCache.count {
                        self.eventsCache[i] = self.enrichEvent(self.eventsCache[i], dict: dict)
                    }
                    for line in self.eventsCache.suffix(self.eventsCache.count - previousCount) {
                        if line != self.lastEventLine {
                            self.postNotification(title: "APC UPS", body: line)
                            self.sendTelegram(body: line)
                            self.lastEventLine = line
                        }
                    }
                }
                self.eventsWindow?.update(with: self.eventsCache)
                // Atualiza janela de autotestes se aberta
                if let stWin = self.selfTestsWindow {
                    stWin.update(with: self.extractSelfTestEvents(from: self.eventsCache))
                }
            }
        }
    }

    @objc func showStatus() {
        if statusWindow == nil {
            let contentView = StatusView(fetchStatus: { [weak self] in
                guard let self = self else { return [:] }
                let (_, base) = self.client.fetchStatus()
                var dict = base
                if let start = self.onBatteryStart {
                    dict["ONBATT_ELAPSED"] = self.formatDuration(Date().timeIntervalSince(start))
                } else if self.lastOnBatteryDuration > 0 {
                    dict["ONBATT_LAST"] = self.formatDuration(self.lastOnBatteryDuration)
                }
                return dict
            })
            let hostingController = NSHostingController(rootView: contentView)
            let window = NSWindow(contentViewController: hostingController)
            window.title = "Status"
            window.setContentSize(NSSize(width: 480, height: 520))
            window.styleMask = [.titled, .closable, .resizable]
            // Follow system appearance (do not force .aqua)
            statusWindow = window
        }
        statusWindow?.makeKeyAndOrderFront(nil)
    }
    @objc func showEvents() {
        if eventsWindow == nil { eventsWindow = EventsWindowController() }
        if let win = eventsWindow {
            win.showWindow(nil)
            win.window?.makeKeyAndOrderFront(nil)
            win.update(with: eventsCache)
        }
    }
    @objc func showGraphs() {
        if graphsWindow == nil { graphsWindow = GraphsWindowController(store: metrics) }
        if let win = graphsWindow {
            win.showWindow(nil)
            win.window?.makeKeyAndOrderFront(nil)
        }
    }
    @objc func showConfig() {
        let alert = NSAlert()
    alert.messageText = "Configuração"
    alert.informativeText = "Editar parâmetros de conexão, intervalo, alertas e bateria."
        alert.addButton(withTitle: "Salvar")
        alert.addButton(withTitle: "Cancelar")

        let hostField = NSTextField(string: Settings.shared.host)
        hostField.placeholderString = "Host"
        hostField.isEditable = true
        hostField.isSelectable = true
        
        let portField = NSTextField(string: String(Settings.shared.port))
        portField.placeholderString = "Porta"
        portField.isEditable = true
        portField.isSelectable = true
        
        let refreshField = NSTextField(string: String(Settings.shared.refreshSeconds))
        refreshField.placeholderString = "Intervalo (s)"
        refreshField.isEditable = true
        refreshField.isSelectable = true

        let tgEnabled = NSButton(checkboxWithTitle: "Enviar alertas via Telegram", target: nil, action: nil)
        tgEnabled.state = Settings.shared.telegramEnabled ? .on : .off
        
        let tgTokenField = NSTextField(string: Settings.shared.telegramBotToken)
        tgTokenField.placeholderString = "Bot Token"
        tgTokenField.isEditable = true
        tgTokenField.isSelectable = true
        
        let tgChatField = NSTextField(string: Settings.shared.telegramChatId)
        tgChatField.placeholderString = "Chat ID"
        tgChatField.isEditable = true
        tgChatField.isSelectable = true

        // Monitoramento tensão/frequência
        let voltageAlertsBox = NSButton(checkboxWithTitle: "Monitorar tensão/frequência", target: nil, action: nil)
        voltageAlertsBox.state = Settings.shared.voltageAlertsEnabled ? .on : .off
        let highVoltField = NSTextField(string: String(Settings.shared.voltageHigh))
        highVoltField.placeholderString = "Tensão alta (V)"
        highVoltField.isEditable = true; highVoltField.isSelectable = true
        let lowVoltField = NSTextField(string: String(Settings.shared.voltageLow))
        lowVoltField.placeholderString = "Tensão baixa (V)"
        lowVoltField.isEditable = true; lowVoltField.isSelectable = true
        let lowFreqField = NSTextField(string: String(Settings.shared.frequencyLow))
        lowFreqField.placeholderString = "Freq. baixa (Hz)"
        lowFreqField.isEditable = true; lowFreqField.isSelectable = true
        let highFreqField = NSTextField(string: String(Settings.shared.frequencyHigh))
        highFreqField.placeholderString = "Freq. alta (Hz)"
        highFreqField.isEditable = true; highFreqField.isSelectable = true

        // Campos de bateria/UPS
        let upsWattsField = NSTextField(string: String(Settings.shared.upsNominalWatts))
        upsWattsField.placeholderString = "Potência nominal (W)"
        let battVoltField = NSTextField(string: String(Settings.shared.batteryNominalVoltage))
        battVoltField.placeholderString = "Bateria nominal (V)"
        let battAhField = NSTextField(string: String(Settings.shared.batteryNominalAh))
        battAhField.placeholderString = "Bateria nominal (Ah)"
    let pfField = NSTextField(string: String(format: "%.2f", Settings.shared.assumedPowerFactor))
    pfField.placeholderString = "Fator de potência assumido (ex: 0.65)"
        let replaceDateString: String = {
            let ts = Settings.shared.batteryReplacedEpoch
            if ts > 0 { return DateFormatter.cached.string(from: Date(timeIntervalSince1970: ts)) }
            return ""
        }()
        let replaceDateField = NSTextField(string: replaceDateString)
        replaceDateField.placeholderString = "yyyy-MM-dd"

        let stack = NSStackView()
        stack.orientation = .vertical
        stack.spacing = 4
        func labeled(_ title: String, _ field: NSView) -> NSView {
            let h = NSStackView()
            h.orientation = .horizontal
            h.spacing = 6
            let label = NSTextField(labelWithString: title)
            label.alignment = .right
            label.font = .systemFont(ofSize: 12)
            label.setContentHuggingPriority(.defaultHigh, for: .horizontal)
            NSLayoutConstraint.activate([label.widthAnchor.constraint(greaterThanOrEqualToConstant: 100)])
            h.addArrangedSubview(label)
            if let tf = field as? NSTextField {
                tf.setContentHuggingPriority(.defaultLow, for: .horizontal)
                NSLayoutConstraint.activate([tf.widthAnchor.constraint(greaterThanOrEqualToConstant: 200)])
            }
            h.addArrangedSubview(field)
            return h
        }
        stack.addArrangedSubview(labeled("Host", hostField))
        stack.addArrangedSubview(labeled("Porta", portField))
        stack.addArrangedSubview(labeled("Intervalo (s)", refreshField))
        // Bateria/UPS
        stack.addArrangedSubview(labeled("Potência nominal (W)", upsWattsField))
        stack.addArrangedSubview(labeled("Bateria nominal (V)", battVoltField))
        stack.addArrangedSubview(labeled("Bateria nominal (Ah)", battAhField))
    stack.addArrangedSubview(labeled("Troca da bateria (yyyy-MM-dd)", replaceDateField))
    stack.addArrangedSubview(labeled("Fator de Potência (assumido)", pfField))
    let sep = NSBox()
    sep.boxType = .separator
    stack.addArrangedSubview(sep)
        stack.addArrangedSubview(tgEnabled)
        stack.addArrangedSubview(labeled("Bot Token", tgTokenField))
        stack.addArrangedSubview(labeled("Chat ID", tgChatField))
        stack.addArrangedSubview(voltageAlertsBox)
        stack.addArrangedSubview(labeled("Tensão Alta", highVoltField))
        stack.addArrangedSubview(labeled("Tensão Baixa", lowVoltField))
        stack.addArrangedSubview(labeled("Freq. Baixa", lowFreqField))
        stack.addArrangedSubview(labeled("Freq. Alta", highFreqField))
        stack.translatesAutoresizingMaskIntoConstraints = false
    stack.setFrameSize(NSSize(width: 440, height: 360))

        alert.accessoryView = stack
        let response = alert.runModal()
        if response == .alertFirstButtonReturn {
            let newHost = hostField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
            let portInt = Int(portField.stringValue) ?? Settings.shared.port
            let refreshInt = Int(refreshField.stringValue) ?? Settings.shared.refreshSeconds
            // Validações
            let validHost = newHost.isEmpty ? Settings.shared.host : newHost
            let validPort = (1...65535).contains(portInt) ? portInt : Settings.shared.port
            let validRefresh = max(2, min(3600, refreshInt))
            Settings.shared.host = validHost
            Settings.shared.port = validPort
            Settings.shared.refreshSeconds = validRefresh
            Settings.shared.telegramEnabled = (tgEnabled.state == .on)
            Settings.shared.telegramBotToken = tgTokenField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
            Settings.shared.telegramChatId = tgChatField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
            // UPS/Bateria persistência
            let upsW = Double(upsWattsField.stringValue.replacingOccurrences(of: ",", with: ".")) ?? Settings.shared.upsNominalWatts
            let bV = Double(battVoltField.stringValue.replacingOccurrences(of: ",", with: ".")) ?? Settings.shared.batteryNominalVoltage
            let bAh = Double(battAhField.stringValue.replacingOccurrences(of: ",", with: ".")) ?? Settings.shared.batteryNominalAh
            let pfVal = Double(pfField.stringValue.replacingOccurrences(of: ",", with: ".")) ?? Settings.shared.assumedPowerFactor
            let prevReplace = Settings.shared.batteryReplacedEpoch
            var newReplace = prevReplace
            let repStr = replaceDateField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
            if !repStr.isEmpty, let dt = DateFormatter.cached.date(from: repStr) { newReplace = dt.timeIntervalSince1970 }
            // Se data de troca mudou para mais recente, resetar contadores/estimativas
            if newReplace > 0 && newReplace != prevReplace {
                Settings.shared.cycleCount = 0
                Settings.shared.estimatedCapacityAh = 0
                Settings.shared.estimatedCapacitySamples = 0
            }
            Settings.shared.upsNominalWatts = max(50, upsW)
            Settings.shared.batteryNominalVoltage = max(6, bV)
            Settings.shared.batteryNominalAh = max(1, bAh)
            Settings.shared.batteryReplacedEpoch = newReplace
            Settings.shared.assumedPowerFactor = min(1.0, max(0.4, pfVal))
            // Parse thresholds
            let vh = Double(highVoltField.stringValue) ?? Settings.shared.voltageHigh
            let vl = Double(lowVoltField.stringValue) ?? Settings.shared.voltageLow
            let fl = Double(lowFreqField.stringValue) ?? Settings.shared.frequencyLow
            let fh = Double(highFreqField.stringValue) ?? Settings.shared.frequencyHigh
            Settings.shared.voltageHigh = max(200, min(300, vh))
            Settings.shared.voltageLow = max(100, min(Settings.shared.voltageHigh - 10, vl))
            Settings.shared.frequencyLow = max(40, min(Settings.shared.frequencyHigh - 1, fl))
            Settings.shared.frequencyHigh = max(Settings.shared.frequencyLow + 1, min(70, fh))
            Settings.shared.voltageAlertsEnabled = (voltageAlertsBox.state == .on)
            Settings.shared.save()
            // Recriar cliente e timer
            client = NisClient(host: Settings.shared.host, port: Settings.shared.port)
            startTimer()
            simpleAlert(title: "Salvo", message: "Host: \(validHost) Porta: \(validPort) Intervalo: \(validRefresh)s\nTelegram: \(Settings.shared.telegramEnabled ? "On" : "Off")\nMonitoração tensão/freq: \(Settings.shared.voltageAlertsEnabled ? "On" : "Off")")
        }
    }
    @objc func runSelfTestPlaceholder() { simpleAlert(title: "Autoteste", message: "Função será integrada (NIS ou apctest)") }
    @objc func quit() { NSApp.terminate(nil) }

    // Filtra eventos que representam autotestes / resultados
    func extractSelfTestEvents(from all: [String]) -> [String] {
        // Procurar padrões comuns: SELFTEST, SELF-TEST, SELF TEST, AUTO TEST, TEST PASSED/FAILED
        let patterns = ["SELFTEST", "SELF-TEST", "SELF TEST", "AUTO TEST", "TEST PASSED", "TEST FAILED", "SELFTEST PASSED", "SELFTEST FAILED"]
        return all.filter { line in
            let upper = line.uppercased()
            for p in patterns { if upper.contains(p) { return true } }
            return false
        }
    }

    @objc func showSelfTests() {
        if selfTestsWindow == nil { selfTestsWindow = SelfTestsWindowController() }
        guard let win = selfTestsWindow else { return }
        win.showWindow(nil)
        win.window?.makeKeyAndOrderFront(nil)
        win.update(with: extractSelfTestEvents(from: eventsCache))
    }

    func currentSummary() -> String {
        let (_, dict) = client.fetchStatus()
        return dict.map { "\($0.key): \($0.value)" }.sorted().joined(separator: "\n")
    }

    // fetchEvents() removido; lógica movida para NisClient

    func simpleAlert(title: String, message: String) {
        let alert = NSAlert()
        alert.messageText = title
        alert.informativeText = message
        alert.runModal()
    }

    func makeIcon(system: String) -> NSImage? {
        if #available(macOS 11.0, *) { return NSImage(systemSymbolName: system, accessibilityDescription: nil) }
        return NSImage(size: NSSize(width: 16, height: 16))
    }

    private func emojiForStatus(_ status: String) -> String {
        let s = status.uppercased()
        if s.contains("COMMLOST") { return "❌" }
        if s.contains("ONBATT") { return "🔋" }
        if s.contains("ONLINE") { return "🔌" }
        if s.contains("CHARG") { return "🔄" }
        return "ℹ️"
    }

    private func formatDuration(_ seconds: TimeInterval) -> String {
        var total = max(0, Int(seconds.rounded()))
        let h = total / 3600
        total %= 3600
        let m = total / 60
        let s = total % 60
        if h > 0 {
            return String(format: "%d:%02d:%02d", h, m, s)
        } else {
            return String(format: "%02d:%02d", m, s)
        }
    }

    // Map event line to an emoji (used to enrich events list & notifications)
    private func eventEmoji(for line: String) -> String {
        let l = line.uppercased()
        if l.contains("COMMLOST") { return "❌" }
        if l.contains("ONBATT") || l.contains("ENTROU EM BATERIA") { return "🔋" }
        if l.contains("SAIU DA BATERIA") || l.contains("ONLINE") { return "🔌" }
        if l.contains("SOBRETENSÃO") || l.contains("SUBTENSÃO") { return "⚡️" }
        if l.contains("FREQ ") || l.contains("FREQ ALTA") || l.contains("FREQ BAIXA") { return "📶" }
        if l.contains("NORMALIZADA") { return "✅" }
        if l.contains("CHARG") { return "🔄" }
        return "ℹ️"
    }

    // Enrich raw event line with contextual metrics and emoji; may become multi-line
    private func enrichEvent(_ line: String, dict: [String:String]) -> String {
        // Prevent double enrichment if already multi-line
        if line.contains("\n") { return line }
        let emoji = eventEmoji(for: line)
        // Extract metrics
        let charge = dict["BCHARGE"] ?? "--"
        let load = dict["LOADPCT"] ?? "--"
        let linev = dict["LINEV"]?.split(separator: " ").first.map(String.init) ?? dict["LINEV"] ?? "--"
        let linefreq = dict["LINEFREQ"]?.split(separator: " ").first.map(String.init) ?? dict["LINEFREQ"] ?? "--"
        let timeLeft = dict["TIMELEFT"] ?? "--"
        var contextParts: [String] = []
        let u = line.uppercased()
        if u.contains("ALERTA") {
            contextParts.append("Bateria: \(charge)")
            contextParts.append("Carga: \(load)")
            contextParts.append("Volt: \(linev)V")
            contextParts.append("Freq: \(linefreq)Hz")
        } else if u.contains("ENTROU EM BATERIA") {
            contextParts.append("Autonomia: \(timeLeft)")
            contextParts.append("Bateria: \(charge)")
            contextParts.append("Carga: \(load)")
        } else if u.contains("SAIU DA BATERIA") {
            contextParts.append("Bateria: \(charge)")
            contextParts.append("Volt: \(linev)V")
            contextParts.append("Freq: \(linefreq)Hz")
        } else if u.contains("MUDANÇA - STATUS") || u.contains("INICIALIZADO - STATUS") {
            contextParts.append("Bateria: \(charge)")
            contextParts.append("Carga: \(load)")
            contextParts.append("Volt/Freq: \(linev)V / \(linefreq)Hz")
            contextParts.append("Autonomia: \(timeLeft)")
        } else if u.contains("NORMALIZADA") {
            contextParts.append("Volt: \(linev)V")
            contextParts.append("Freq: \(linefreq)Hz")
            contextParts.append("Bateria: \(charge)")
        } else {
            // Generic fallback
            contextParts.append("Bateria: \(charge)")
            contextParts.append("Carga: \(load)")
        }
        let contextLine = contextParts.joined(separator: " | ")
        // Add emoji prefix if missing
        let prefixed = line.hasPrefix(emoji) ? line : "\(emoji) \(line)"
        return "\(prefixed)\n\(contextLine)"
    }

    func postNotification(title: String, body: String) {
        guard canUseUserNotifications() else {
            print("[Notifications] Skipped (not in .app bundle): \(title) - \(body)")
            return
        }
        let content = UNMutableNotificationContent()
        // Use cached UPS name from last poll for performance
        content.title = lastUpsName
        content.body = body
        content.sound = .default
        let request = UNNotificationRequest(identifier: UUID().uuidString, content: content, trigger: nil)
        UNUserNotificationCenter.current().add(request) { error in
            if let error = error {
                print("[Notifications] Error sending: \(error)")
            } else {
                print("[Notifications] Sent: \(title) - \(body)")
            }
        }
    }

    private func sendTelegram(body: String) {
        let s = Settings.shared
        guard s.telegramEnabled, !s.telegramBotToken.isEmpty, !s.telegramChatId.isEmpty else {
            print("[Telegram] Skipped (not enabled or missing credentials)")
            return
        }
        let token = s.telegramBotToken
        guard let url = URL(string: "https://api.telegram.org/bot\(token)/sendMessage") else {
            print("[Telegram] Invalid bot token URL")
            return
        }
        var req = URLRequest(url: url)
        req.httpMethod = "POST"
        req.setValue("application/json", forHTTPHeaderField: "Content-Type")
        // Prefix message with status emoji when appropriate
    let knownEmojiPrefixes = ["❌","🔋","🔌","🔄","ℹ️","⚠️","✅","⚡️","⚡","📶"]
        let trimmed = body.trimmingCharacters(in: .whitespaces)
        let hasEmojiPrefix = knownEmojiPrefixes.contains { trimmed.hasPrefix($0) }
        let emoji = emojiForTelegramMessage(body)
    // Structure: Cached device name first line, status/event (with emoji) second
    let deviceName = lastUpsName
    let secondLine = hasEmojiPrefix ? body : "\(emoji) \(body)"
    let finalText = "\(deviceName)\n\(secondLine)"
        let payload: [String: Any] = [
            "chat_id": s.telegramChatId,
            "text": finalText
        ]
        req.httpBody = try? JSONSerialization.data(withJSONObject: payload, options: [])
        print("[Telegram] Sending to chat \(s.telegramChatId): \(finalText)")
        URLSession.shared.dataTask(with: req) { data, response, error in
            if let error = error {
                print("[Telegram] Error: \(error.localizedDescription)")
                return
            }
            if let httpResponse = response as? HTTPURLResponse {
                print("[Telegram] HTTP \(httpResponse.statusCode)")
                if httpResponse.statusCode != 200, let data = data, let responseText = String(data: data, encoding: .utf8) {
                    print("[Telegram] Response: \(responseText)")
                }
            }
        }.resume()
    }

    private func emojiForTelegramMessage(_ body: String) -> String {
        let b = body.uppercased()
        if b.contains("COMMLOST") { return "❌" }
        if b.contains("ONBATT") || b.contains("EM BATERIA") || b.contains("ENTROU EM BATERIA") { return "🔋" }
        if b.contains("NORMALIZADA") { return "✅" }
        if b.contains("ONLINE") || b.contains("SAIU DA BATERIA") { return "🔌" }
        if b.contains("SOBRETENSÃO") || b.contains("SUBTENSÃO") || b.contains("LINEV") { return "⚡️" }
        if b.contains("FREQ") { return "📶" }
        if b.contains("CHARG") { return "🔄" }
        return "ℹ️"
    }

    // Somente disponível quando rodando como .app (Bundle válido) – evita crash do UNUserNotificationCenter
    private func canUseUserNotifications() -> Bool {
        // bundleIdentifier e sufixo .app são bons indicativos de app empacotado
        if Bundle.main.bundleIdentifier == nil { return false }
        let path = Bundle.main.bundleURL.path.lowercased()
        return path.hasSuffix(".app")
    }

    @objc func sendTestNotification() {
        let msg = "Notificação de teste"
        postNotification(title: "APC UPS", body: msg)
        sendTelegram(body: msg)
    }
    @objc func clearEvents() {
        eventsCache.removeAll()
        lastEventLine = ""
        lastStatusText = ""
        lastVoltageAlerted = false
        lastFrequencyAlerted = false
        eventsWindow?.update(with: eventsCache)
    }

    @objc func openNotificationSettings() {
        // Try modern and legacy URLs
        let candidates = [
            "x-apple.systempreferences:com.apple.Notifications-Settings.extension",
            "x-apple.systempreferences:com.apple.preference.notifications"
        ]
        for c in candidates {
            if let url = URL(string: c), NSWorkspace.shared.open(url) { return }
        }
        // Fallback: open System Settings / System Preferences depending on OS version
        if #available(macOS 13.0, *) { // Ventura+: System Settings
            if let url = URL(string: "x-apple.systempreferences:") { _ = NSWorkspace.shared.open(url) }
        } else if #available(macOS 10.15, *) { // Use modern API to open System Preferences
            if let appURL = NSWorkspace.shared.urlForApplication(withBundleIdentifier: "com.apple.systempreferences") {
                let config = NSWorkspace.OpenConfiguration()
                NSWorkspace.shared.openApplication(at: appURL, configuration: config, completionHandler: nil)
            } else {
                NSWorkspace.shared.launchApplication("System Preferences") // last resort
            }
        } else {
            NSWorkspace.shared.launchApplication("System Preferences")
        }
    }

    private func timestamp() -> String {
        let f = DateFormatter()
        f.dateFormat = "yyyy-MM-dd HH:mm:ss"
        return f.string(from: Date())
    }
}

// MARK: - Settings persistence
final class Settings {
    static let shared = Settings()
    private init() {}
    private let kHost = "nisHost"
    private let kPort = "nisPort"
    private let kRefresh = "nisRefresh"
    private let kTgEnabled = "tgEnabled"
    private let kTgToken = "tgToken"
    private let kTgChat = "tgChat"
    private let kVoltHigh = "voltHigh"
    private let kVoltLow = "voltLow"
    private let kFreqLow = "freqLow"
    private let kFreqHigh = "freqHigh"
    private let kVoltageAlerts = "voltageAlerts"
    private let kOnBattStart = "onBattStartEpoch"
    private let kLastOnBatt = "lastOnBattSeconds"
    private let kDailyLogHour = "dailyLogHour"
    private let kUpsNominalWatts = "upsNominalWatts"
    private let kCycleCount = "cycleCount"
    private let kBattNominalV = "batteryNominalVoltage"
    private let kBattNominalAh = "batteryNominalAh"
    private let kBattReplacedEpoch = "batteryReplacedEpoch"
    private let kEstCapacityAh = "estimatedCapacityAh"
    private let kEstCapacitySamples = "estimatedCapacitySamples"
    private let kAssumedPF = "assumedPowerFactor"
    var dailyLogHour: Int = 8
    var host: String = "127.0.0.1"
    var port: Int = 3551
    var refreshSeconds: Int = 10
    var telegramEnabled: Bool = false
    var telegramBotToken: String = ""
    var telegramChatId: String = ""
    // Monitoring thresholds (defaults for 127V / 60Hz network)
    var voltageHigh: Double = 140.0  // 127V network: upper limit
    var voltageLow: Double = 105.0   // 127V network: lower limit
    var frequencyLow: Double = 58.0  // 60Hz network: lower limit
    var frequencyHigh: Double = 62.0 // 60Hz network: upper limit
    var voltageAlertsEnabled: Bool = true
    // Persisted on-battery timing
    var onBattStartEpoch: TimeInterval = 0
    var lastOnBattSeconds: TimeInterval = 0
    // UPS/battery parameters
    var upsNominalWatts: Double = 600.0
    var batteryNominalVoltage: Double = 24.0
    var batteryNominalAh: Double = 7.0
    var batteryReplacedEpoch: TimeInterval = 0
    // Estimated capacity tracking
    var estimatedCapacityAh: Double = 0
    var estimatedCapacitySamples: Int = 0
    // Battery cycles count
    var cycleCount: Int = 0
    // Assumed power factor used when only VA is available
    var assumedPowerFactor: Double = 0.65

    func load() {
        let d = UserDefaults.standard
        if let h = d.string(forKey: kHost) { host = h }
        let p = d.integer(forKey: kPort); if p != 0 { port = p }
        let r = d.integer(forKey: kRefresh); if r != 0 { refreshSeconds = r }
        telegramEnabled = d.object(forKey: kTgEnabled) as? Bool ?? false
        if let t = d.string(forKey: kTgToken) { telegramBotToken = t }
        if let c = d.string(forKey: kTgChat) { telegramChatId = c }
        let vh = d.double(forKey: kVoltHigh); if vh != 0 { voltageHigh = vh }
        let vl = d.double(forKey: kVoltLow); if vl != 0 { voltageLow = vl }
        let fl = d.double(forKey: kFreqLow); if fl != 0 { frequencyLow = fl }
        let fh = d.double(forKey: kFreqHigh); if fh != 0 { frequencyHigh = fh }
        voltageAlertsEnabled = d.object(forKey: kVoltageAlerts) as? Bool ?? voltageAlertsEnabled
        let start = d.double(forKey: kOnBattStart); if start != 0 { onBattStartEpoch = start }
        let last = d.double(forKey: kLastOnBatt); if last != 0 { lastOnBattSeconds = last }
    let dailyHour = d.integer(forKey: kDailyLogHour); if dailyHour != 0 { dailyLogHour = dailyHour }
    let cycles = d.integer(forKey: kCycleCount); if cycles != 0 { cycleCount = cycles }
    let upsW = d.double(forKey: kUpsNominalWatts); if upsW != 0 { upsNominalWatts = upsW }
    let bV = d.double(forKey: kBattNominalV); if bV != 0 { batteryNominalVoltage = bV }
    let bAh = d.double(forKey: kBattNominalAh); if bAh != 0 { batteryNominalAh = bAh }
    let rep = d.double(forKey: kBattReplacedEpoch); if rep != 0 { batteryReplacedEpoch = rep }
    let estAh = d.double(forKey: kEstCapacityAh); if estAh != 0 { estimatedCapacityAh = estAh }
    let estN = d.integer(forKey: kEstCapacitySamples); if estN != 0 { estimatedCapacitySamples = estN }
    let pf = d.double(forKey: kAssumedPF); if pf != 0 { assumedPowerFactor = pf }
    }
    func save() {
        let d = UserDefaults.standard
        d.set(host, forKey: kHost)
        d.set(port, forKey: kPort)
        d.set(refreshSeconds, forKey: kRefresh)
        d.set(telegramEnabled, forKey: kTgEnabled)
        d.set(telegramBotToken, forKey: kTgToken)
        d.set(telegramChatId, forKey: kTgChat)
        d.set(voltageHigh, forKey: kVoltHigh)
        d.set(voltageLow, forKey: kVoltLow)
        d.set(frequencyLow, forKey: kFreqLow)
        d.set(frequencyHigh, forKey: kFreqHigh)
        d.set(voltageAlertsEnabled, forKey: kVoltageAlerts)
    d.set(onBattStartEpoch, forKey: kOnBattStart)
    d.set(lastOnBattSeconds, forKey: kLastOnBatt)
    d.set(dailyLogHour, forKey: kDailyLogHour)
    d.set(cycleCount, forKey: kCycleCount)
    d.set(upsNominalWatts, forKey: kUpsNominalWatts)
    d.set(batteryNominalVoltage, forKey: kBattNominalV)
    d.set(batteryNominalAh, forKey: kBattNominalAh)
    d.set(batteryReplacedEpoch, forKey: kBattReplacedEpoch)
    d.set(estimatedCapacityAh, forKey: kEstCapacityAh)
    d.set(estimatedCapacitySamples, forKey: kEstCapacitySamples)
    d.set(assumedPowerFactor, forKey: kAssumedPF)
    }
}

// MARK: - Status View (SwiftUI)
struct StatusView: View {
    let fetchStatus: () -> [String: String]
    @State private var statusData: [String: String] = [:]
    @State private var timer: Timer?
    @Environment(\.colorScheme) var colorScheme
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 12) {
                // Header
                HStack {
                    if #available(macOS 11.0, *) {
                        Image(systemName: iconName)
                            .font(.system(size: 32))
                            .foregroundColor(iconColor)
                    }
                    VStack(alignment: .leading) {
                        Text(statusData["UPSNAME"] ?? "UPS")
                            .font(.title2)
                            .fontWeight(.semibold)
                        let statusRaw = statusData["STATUS"] ?? "Desconhecido"
                        Text("\(statusEmoji) \(statusRaw)")
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                        if let elapsed = statusData["ONBATT_ELAPSED"] {
                            Text("🔋 Em bateria: \(elapsed)")
                                .font(.caption)
                                .foregroundColor(.orange)
                        } else if let last = statusData["ONBATT_LAST"] {
                            Text("🔋 Último ciclo: \(last)")
                                .font(.caption)
                                .foregroundColor(.secondary)
                        }
                    }
                    Spacer()
                    Button("Atualizar") { refresh() }
                }
                .padding()
                .background(cardBackground)
                .cornerRadius(8)
                
                // Battery, Load, Cycles & Capacity
                HStack(spacing: 16) {
                    MetricCard(
                        title: "Bateria",
                        value: statusData["BCHARGE"] ?? "--",
                        icon: "battery.75",
                        colorScheme: colorScheme
                    )
                    MetricCard(
                        title: "Carga",
                        value: statusData["LOADPCT"] ?? "--",
                        icon: "gauge",
                        colorScheme: colorScheme
                    )
                    MetricCard(
                        title: "Ciclos",
                        value: String(Settings.shared.cycleCount),
                        icon: "arrow.2.circlepath",
                        colorScheme: colorScheme
                    )
                    MetricCard(
                        title: "Capac. Est.",
                        value: Settings.shared.estimatedCapacityAh > 0 ? String(format: "%.1f Ah", Settings.shared.estimatedCapacityAh) : "--",
                        icon: "bolt.circle",
                        colorScheme: colorScheme
                    )
                    MetricCard(
                        title: "Ciclos",
                        value: String(Settings.shared.cycleCount),
                        icon: "arrow.2.circlepath",
                        colorScheme: colorScheme
                    )
                }
                
                // Voltage & Frequency
                HStack(spacing: 16) {
                    MetricCard(
                        title: "Entrada",
                        value: statusData["LINEV"] ?? "--",
                        icon: "arrow.down.circle",
                        colorScheme: colorScheme
                    )
                    MetricCard(
                        title: "Saída",
                        value: statusData["OUTPUTV"] ?? "--",
                        icon: "arrow.up.circle",
                        colorScheme: colorScheme
                    )
                }
                
                HStack(spacing: 16) {
                    MetricCard(
                        title: "Frequência",
                        value: statusData["LINEFREQ"] ?? "--",
                        icon: "waveform",
                        colorScheme: colorScheme
                    )
                    MetricCard(
                        title: "Tempo Rest.",
                        value: statusData["TIMELEFT"] ?? "--",
                        icon: "clock",
                        colorScheme: colorScheme
                    )
                }
                
                // Detailed Info
                Divider().padding(.vertical, 4)
                Text("Detalhes").font(.headline).padding(.horizontal)
                ForEach(sortedKeys, id: \.self) { key in
                    HStack {
                        Text(key).font(.caption).foregroundColor(.secondary)
                        Spacer()
                        Text(statusData[key] ?? "").font(.caption).lineLimit(1)
                    }
                    .padding(.horizontal)
                }
            }
            .padding()
        }
        .frame(minWidth: 400, minHeight: 480)
        .background(Color(red: 0.03, green: 0.05, blue: 0.10))
        .preferredColorScheme(.dark)
        .onAppear {
            refresh()
            // Start auto-refresh timer when view appears
            startAutoRefresh()
        }
        .onDisappear {
            // Stop timer when view disappears
            timer?.invalidate()
            timer = nil
        }
    }
    
    private var sortedKeys: [String] {
        statusData.keys.sorted()
    }
    
    private var iconName: String {
        let status = statusData["STATUS"] ?? ""
        if status.contains("COMMLOST") { return "bolt.slash" }
        if status.contains("ONBATT") { return "bolt.fill" }
        if status.contains("ONLINE") { return "checkmark.circle" }
        return "battery.75"
    }
    
    private var iconColor: Color {
        let status = statusData["STATUS"] ?? ""
        if status.contains("COMMLOST") { return .red }
        if status.contains("ONBATT") { return .orange }
        if status.contains("ONLINE") { return .green }
        return .blue
    }
    
    private var statusEmoji: String {
        let s = (statusData["STATUS"] ?? "").uppercased()
        if s.contains("COMMLOST") { return "❌" }
        if s.contains("ONBATT") { return "🔋" }
        if s.contains("ONLINE") { return "🔌" }
        if s.contains("CHARG") { return "🔄" }
        return "ℹ️"
    }
    
    private func refresh() {
        statusData = fetchStatus()
    }
    
    private func startAutoRefresh() {
        timer?.invalidate()
        let interval: TimeInterval = 5.0 // Refresh every 5 seconds
        let t = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { _ in
            refresh()
        }
        timer = t
        RunLoop.main.add(t, forMode: .common)
    }
    
    private var cardBackground: Color {
        colorScheme == .dark ? Color(white: 0.15) : Color(nsColor: .controlBackgroundColor)
    }
}

// DateFormatter otimizado para datas yyyy-MM-dd (file-scope, reutilizável)
extension DateFormatter {
    static let cached: DateFormatter = {
        let f = DateFormatter()
        f.dateFormat = "yyyy-MM-dd"
        f.locale = Locale(identifier: "en_US_POSIX")
        return f
    }()
}

// MARK: - Metrics models and store
struct MetricSample: Identifiable, Codable {
    let id = UUID()
    let time: Date
    let charge: Double?
    let load: Double?
    let lineV: Double?
    let freq: Double?
    
    enum CodingKeys: String, CodingKey {
        case id, time, charge, load, lineV, freq
    }
}

struct MetricTableRow: Identifiable {
    let id = UUID()
    let timestamp: String
    let value: String
}

final class MetricsStore: ObservableObject {
    @Published var samples: [MetricSample] = []
    var maxSamples: Int = 1000
    private var saveTimer: Timer?
    private let saveURL: URL = {
        let dir = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let appDir = dir.appendingPathComponent("ApcCtrl", isDirectory: true)
        try? FileManager.default.createDirectory(at: appDir, withIntermediateDirectories: true)
        return appDir.appendingPathComponent("metrics.json")
    }()
    
    init() {
        load()
        // Auto-save every 5 minutes
        saveTimer = Timer.scheduledTimer(withTimeInterval: 300, repeats: true) { [weak self] _ in
            self?.save()
        }
        RunLoop.main.add(saveTimer!, forMode: .common)
    }
    
    deinit {
        saveTimer?.invalidate()
        save()
    }
    
    func append(_ s: MetricSample) {
        samples.append(s)
        if samples.count > maxSamples {
            samples.removeFirst(samples.count - maxSamples)
        }
    }
    
    private func save() {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        guard let data = try? encoder.encode(samples) else { return }
        try? data.write(to: saveURL)
        print("[MetricsStore] Saved \(samples.count) samples to \(saveURL.path)")
    }
    
    private func load() {
        guard let data = try? Data(contentsOf: saveURL) else { return }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        if let loaded = try? decoder.decode([MetricSample].self, from: data) {
            samples = loaded
            print("[MetricsStore] Loaded \(samples.count) samples from \(saveURL.path)")
        }
    }
}

#if canImport(Charts)
@available(macOS 13.0, *)
struct GraphsView: View {
    @ObservedObject var store: MetricsStore
    enum Metric: String, CaseIterable, Identifiable {
        case charge, load, lineV, freq
        var id: String { rawValue }
        var title: String {
            switch self {
            case .charge: return "Bateria (%)"
            case .load:   return "Carga (%)"
            case .lineV:  return "Tensão (V)"
            case .freq:   return "Frequência (Hz)"
            }
        }
    }
    enum Range: String, CaseIterable, Identifiable { case h1, h6, h24, custom
        var id: String { rawValue }
        var title: String { switch self { case .h1: return "1h"; case .h6: return "6h"; case .h24: return "24h"; case .custom: return "Custom" } }
        var hours: Double { switch self { case .h1: return 1; case .h6: return 6; case .h24: return 24; case .custom: return 6 } }
    }
    @State private var metric: Metric = .charge
    @State private var range: Range = .h6
    @State private var startDate: Date = Calendar.current.date(byAdding: .hour, value: -6, to: Date()) ?? Date().addingTimeInterval(-6*3600)
    @State private var endDate: Date = Date()
    @State private var showTable: Bool = false
    @State private var hoverTime: Date? = nil

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(spacing: 12) {
                Picker("Métrica", selection: $metric) {
                    ForEach(Metric.allCases) { m in Text(m.title).tag(m) }
                }.pickerStyle(.segmented)
                Picker("Janela", selection: $range) {
                    ForEach(Range.allCases) { r in Text(r.title).tag(r) }
                }.pickerStyle(.segmented).frame(width: 240)
                Spacer()
            }
            if range == .custom {
                HStack(spacing: 8) {
                    DatePicker("De:", selection: $startDate, displayedComponents: [.date, .hourAndMinute])
                    DatePicker("Até:", selection: $endDate, displayedComponents: [.date, .hourAndMinute])
                    Spacer()
                }
            }
            Chart(filteredSamples) { s in
                switch metric {
                case .charge:
                    if let y = s.charge { LineMark(x: .value("Hora", s.time), y: .value("%", y)).foregroundStyle(.cyan) }
                case .load:
                    if let y = s.load { LineMark(x: .value("Hora", s.time), y: .value("%", y)).foregroundStyle(.green) }
                case .lineV:
                    if let y = s.lineV { 
                        LineMark(x: .value("Hora", s.time), y: .value("V", y)).foregroundStyle(.orange)
                        RuleMark(y: .value("127V", 127.0))
                            .foregroundStyle(.secondary.opacity(0.5))
                            .lineStyle(StrokeStyle(lineWidth: 1, dash: [4,4]))
                            .annotation(position: .top, alignment: .leading) { 
                                Text("127V").font(.caption).foregroundColor(.secondary)
                            }
                    }
                case .freq:
                    if let y = s.freq { 
                        LineMark(x: .value("Hora", s.time), y: .value("Hz", y)).foregroundStyle(.purple)
                        RuleMark(y: .value("60Hz", 60.0))
                            .foregroundStyle(.secondary.opacity(0.5))
                            .lineStyle(StrokeStyle(lineWidth: 1, dash: [4,4]))
                            .annotation(position: .top, alignment: .leading) { 
                                Text("60Hz").font(.caption).foregroundColor(.secondary)
                            }
                    }
                }
            }
            .chartOverlay { proxy in
                GeometryReader { geo in
                    Rectangle().fill(.clear).onContinuousHover { phase in
                        switch phase {
                        case .active(let p):
                            let x = p.x - geo[proxy.plotAreaFrame].origin.x
                            if let date: Date = proxy.value(atX: x) {
                                hoverTime = date
                            }
                        case .ended:
                            hoverTime = nil
                        }
                    }
                }
            }
            .chartXAxis { AxisMarks(values: .automatic(desiredCount: 6)) }
            .frame(minHeight: 360)
            .background(Color(red: 0.05, green: 0.08, blue: 0.15))
            .cornerRadius(8)
            HStack {
                Toggle("Mostrar tabela", isOn: $showTable)
                Spacer()
                if let h = hoveredSample {
                    Text(hoverText(for: h)).font(.caption).foregroundColor(.secondary)
                }
            }
            if showTable {
                Table(tableRows) {
                    TableColumn("Hora") { row in Text(row.timestamp).foregroundColor(.primary) }
                    TableColumn("Valor") { row in Text(row.value).foregroundColor(.primary) }
                }
                .frame(minHeight: 160)
                .background(Color(red: 0.08, green: 0.11, blue: 0.18))
                .cornerRadius(6)
            }
        }
        .padding(12)
        .background(Color(red: 0.03, green: 0.05, blue: 0.10))
        .preferredColorScheme(.dark)
    }

    private var timeFormatter: DateFormatter {
        let f = DateFormatter()
        f.dateFormat = "yyyy-MM-dd HH:mm:ss"
        return f
    }

    private var hoveredSample: MetricSample? {
        guard let t = hoverTime else { return nil }
        return filteredSamples.min(by: { abs($0.time.timeIntervalSince(t)) < abs($1.time.timeIntervalSince(t)) })
    }
    private func hoverText(for s: MetricSample) -> String {
        let ts = timeFormatter.string(from: s.time)
        switch metric {
        case .charge: return "\(ts)  \(String(format: "%.1f", s.charge ?? .nan)) %"
        case .load:   return "\(ts)  \(String(format: "%.1f", s.load ?? .nan)) %"
        case .lineV:  return "\(ts)  \(String(format: "%.1f", s.lineV ?? .nan)) V"
        case .freq:   return "\(ts)  \(String(format: "%.1f", s.freq ?? .nan)) Hz"
        }
    }
    private var tableRows: [MetricTableRow] {
        filteredSamples.map { s in
            let ts = timeFormatter.string(from: s.time)
            let val: String
            switch metric {
            case .charge: val = String(format: "%.1f %%", s.charge ?? .nan)
            case .load:   val = String(format: "%.1f %%", s.load ?? .nan)
            case .lineV:  val = String(format: "%.1f V", s.lineV ?? .nan)
            case .freq:   val = String(format: "%.1f Hz", s.freq ?? .nan)
            }
            return MetricTableRow(timestamp: ts, value: val)
        }
    }

    private var filteredSamples: [MetricSample] {
        if range == .custom {
            return store.samples.filter { $0.time >= startDate && $0.time <= endDate }
        }
        let cutoff = Date().addingTimeInterval(-range.hours * 3600)
        return store.samples.filter { $0.time >= cutoff }
    }
}
#endif
struct GraphsViewLegacy: View {
    @ObservedObject var store: MetricsStore
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Gráficos requerem macOS 13+ (Swift Charts). Exibindo amostras cruas:")
                .font(.headline)
            ScrollView {
                ForEach(store.samples) { s in
                    Text("\(s.time): Carga=\(s.load ?? .nan), Bateria=\(s.charge ?? .nan), V=\(s.lineV ?? .nan), Hz=\(s.freq ?? .nan)")
                        .font(.system(.caption, design: .monospaced))
                }
            }
        }
        .padding(12)
    }
}

// MARK: - Graphs Window
final class GraphsWindowController: NSWindowController, NSWindowDelegate {
    init(store: MetricsStore) {
        #if canImport(Charts)
        if #available(macOS 13.0, *) {
            let hosting = NSHostingController(rootView: GraphsView(store: store))
            let window = NSWindow(contentViewController: hosting)
            window.title = "Gráficos"
            window.setContentSize(NSSize(width: 720, height: 520))
            window.styleMask = [.titled, .closable, .resizable]
            super.init(window: window)
            window.delegate = self
            return
        }
        #endif
        let hosting = NSHostingController(rootView: GraphsViewLegacy(store: store))
        let window = NSWindow(contentViewController: hosting)
        window.title = "Gráficos"
        window.setContentSize(NSSize(width: 720, height: 520))
        window.styleMask = [.titled, .closable, .resizable]
        super.init(window: window)
        window.delegate = self
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }
}

struct MetricCard: View {
    let title: String
    let value: String
    let icon: String
    let colorScheme: ColorScheme
    
    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                if #available(macOS 11.0, *) {
                    Image(systemName: icon)
                        .foregroundColor(.accentColor)
                }
                Text(title).font(.caption).foregroundColor(.secondary)
            }
            Text(value)
                .font(.title3)
                .fontWeight(.medium)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding()
        .background(cardBackground)
        .cornerRadius(6)
    }
    
    private var cardBackground: Color {
        colorScheme == .dark ? Color(white: 0.15) : Color(nsColor: .controlBackgroundColor)
    }
}

// MARK: - Events Window
final class EventsWindowController: NSWindowController, NSWindowDelegate {
    private let textView = NSTextView(frame: .zero)
    private var appearanceObserver: NSObjectProtocol?
    private var kvoAppearanceObservation: NSKeyValueObservation?

    init() {
        let rect = NSRect(x: 0, y: 0, width: 560, height: 420)
        let window = NSWindow(contentRect: rect, styleMask: [.titled, .closable, .resizable], backing: .buffered, defer: false)
        window.title = "Eventos"
        super.init(window: window)
    window.delegate = self
    // Do not override appearance; let system decide (enables dark mode automatically)

        let scroll = NSScrollView(frame: window.contentView?.bounds ?? rect)
        scroll.autoresizingMask = [.width, .height]
        scroll.hasVerticalScroller = true
        scroll.hasHorizontalScroller = true

        textView.isEditable = false
        textView.drawsBackground = true
        if #available(macOS 13.0, *) {
            textView.font = .monospacedSystemFont(ofSize: 12, weight: .regular)
        } else {
            textView.font = .systemFont(ofSize: 12)
        }
        textView.textContainerInset = NSSize(width: 6, height: 6)
        updateColors()
        scroll.documentView = textView
        window.contentView?.addSubview(scroll)

        // Update colors on appearance changes using KVO (10.14+). Older systems don't support dark mode.
        if #available(macOS 10.14, *) {
            kvoAppearanceObservation = window.observe(\.effectiveAppearance, options: [.new]) { [weak self] _, _ in
                self?.updateColors()
            }
        }
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    deinit {
        if let obs = appearanceObserver {
            NotificationCenter.default.removeObserver(obs)
        }
        kvoAppearanceObservation = nil
    }

    func update(with lines: [String]) {
        updateColors()
        if lines.isEmpty {
            textView.string = "(Nenhum evento ainda. Aguardando mudanças de STATUS...)"
        } else {
            textView.string = lines.joined(separator: "\n")
        }
        textView.scrollToEndOfDocument(nil)
    }
    
    private func updateColors() {
    let isDark = NSApp.effectiveAppearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua
    textView.backgroundColor = isDark ? NSColor.windowBackgroundColor.blended(withFraction: 0.15, of: .black)! : NSColor.textBackgroundColor
    textView.textColor = isDark ? .white : .textColor
    }
}

// MARK: - Self Tests Window
final class SelfTestsWindowController: NSWindowController, NSWindowDelegate {
    private let textView = NSTextView(frame: .zero)
    private var kvoAppearanceObservation: NSKeyValueObservation?

    init() {
        let rect = NSRect(x: 0, y: 0, width: 520, height: 360)
        let window = NSWindow(contentRect: rect, styleMask: [.titled, .closable, .resizable], backing: .buffered, defer: false)
        window.title = "Autotestes"
        super.init(window: window)
        window.delegate = self

        let scroll = NSScrollView(frame: window.contentView?.bounds ?? rect)
        scroll.autoresizingMask = [.width, .height]
        scroll.hasVerticalScroller = true
        textView.isEditable = false
        if #available(macOS 13.0, *) { textView.font = .monospacedSystemFont(ofSize: 12, weight: .regular) } else { textView.font = .systemFont(ofSize: 12) }
        textView.textContainerInset = NSSize(width: 6, height: 6)
        updateColors()
        scroll.documentView = textView
        window.contentView?.addSubview(scroll)
        if #available(macOS 10.14, *) {
            kvoAppearanceObservation = window.observe(\.effectiveAppearance, options: [.new]) { [weak self] _, _ in
                self?.updateColors()
            }
        }
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }
    deinit { kvoAppearanceObservation = nil }

    func update(with lines: [String]) {
        updateColors()
        if lines.isEmpty {
            textView.string = "(Nenhum autoteste encontrado nos eventos.)"
        } else {
            textView.string = lines.joined(separator: "\n")
        }
        textView.scrollToEndOfDocument(nil)
    }

    private func updateColors() {
        let isDark = NSApp.effectiveAppearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua
        textView.backgroundColor = isDark ? NSColor.windowBackgroundColor.blended(withFraction: 0.15, of: .black)! : NSColor.textBackgroundColor
        textView.textColor = isDark ? .white : .textColor
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.run()
