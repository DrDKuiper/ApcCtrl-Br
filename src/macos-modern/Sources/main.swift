// apcctrl macOS modern agent prototype
// GPLv2 - see COPYING in project root
// Minimal menubar app in Swift using AppKit

import Cocoa
import UserNotifications
import Foundation
import SwiftUI

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
    var statusWindow: NSWindow?
    // Track alert states to avoid spamming repeated voltage/frequency notifications
    var lastVoltageAlerted: Bool = false
    var lastFrequencyAlerted: Bool = false

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let button = statusItem.button { button.image = makeIcon(system: "exclamationmark.circle") }
        // Carregar configurações persistidas
        Settings.shared.load()
        client = NisClient(host: Settings.shared.host, port: Settings.shared.port)
        constructMenu()
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
    }

    // Show notifications even when app is foreground
    func userNotificationCenter(_ center: UNUserNotificationCenter, willPresent notification: UNNotification, withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        if #available(macOS 11.0, *) {
            completionHandler([.banner, .list, .sound])
        } else {
            completionHandler([.alert, .sound])
        }
    }
    func startTimer() {
        timer?.invalidate()
        let interval = max(2.0, TimeInterval(Settings.shared.refreshSeconds))
        print("[Timer] Starting with interval: \(interval)s")
        // Create timer and add to main run loop in common mode (keeps firing during menu/window interactions)
        let t = Timer(timeInterval: interval, repeats: true) { [weak self] _ in
            print("[Timer] Poll triggered at \(Date())")
            self?.poll()
        }
        timer = t
        RunLoop.main.add(t, forMode: .common)
    }

    func constructMenu() {
        let menu = NSMenu()
        menu.addItem(NSMenuItem(title: "Status", action: #selector(showStatus), keyEquivalent: "s"))
        menu.addItem(NSMenuItem(title: "Eventos", action: #selector(showEvents), keyEquivalent: "e"))
        menu.addItem(NSMenuItem(title: "Notificação de teste", action: #selector(sendTestNotification), keyEquivalent: "n"))
        menu.addItem(NSMenuItem(title: "Abrir Preferências de Notificações", action: #selector(openNotificationSettings), keyEquivalent: ""))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Configuração", action: #selector(showConfig), keyEquivalent: "c"))
        menu.addItem(NSMenuItem(title: "Limpar eventos", action: #selector(clearEvents), keyEquivalent: "l"))
        menu.addItem(NSMenuItem(title: "Autoteste (em breve)", action: #selector(runSelfTestPlaceholder), keyEquivalent: "t"))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Sair", action: #selector(quit), keyEquivalent: "q"))
        statusItem.menu = menu
    }

    @objc func poll() {
        print("[Poll] Starting fetch at \(Date())")
        // Run fetch in background to avoid blocking timer
        DispatchQueue.global(qos: .userInitiated).async { [weak self] in
            guard let self = self else { return }
            let (state, dict) = self.client.fetchStatus()
            print("[Poll] Fetched status: \(dict["STATUS"] ?? "N/A"), state: \(state)")
            
            // Update UI on main thread
            DispatchQueue.main.async {
                if let button = self.statusItem.button {
                    switch state {
                    case .commlost: button.image = self.makeIcon(system: "bolt.slash")
                    case .onbatt:   button.image = self.makeIcon(system: "bolt.fill")
                    case .charging: button.image = self.makeIcon(system: "battery.50")
                    case .online:   button.image = self.makeIcon(system: "checkmark.circle")
                    }
                    let statusText = dict["STATUS"] ?? "Desconhecido"
                    button.toolTip = "UPS: \(dict["UPSNAME"] ?? "?")\nStatus: \(statusText)"
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
                    for line in self.eventsCache.suffix(self.eventsCache.count - previousCount) {
                        if line != self.lastEventLine {
                            self.postNotification(title: "APC UPS", body: line)
                            self.sendTelegram(body: line)
                            self.lastEventLine = line
                        }
                    }
                }
                self.eventsWindow?.update(with: self.eventsCache)
            }
        }
    }

    @objc func showStatus() {
        if statusWindow == nil {
            let contentView = StatusView(fetchStatus: { [weak self] in
                guard let self = self else { return [:] }
                let (_, dict) = self.client.fetchStatus()
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
    @objc func showConfig() {
        let alert = NSAlert()
        alert.messageText = "Configuração NIS"
        alert.informativeText = "Editar parâmetros de conexão, intervalo e alertas."
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

        let stack = NSStackView()
        stack.orientation = .vertical
        stack.spacing = 4
        func labeled(_ title: String, _ field: NSTextField) -> NSView {
            let h = NSStackView()
            h.orientation = .horizontal
            h.spacing = 6
            let label = NSTextField(labelWithString: title)
            label.alignment = .right
            label.font = .systemFont(ofSize: 12)
            label.setContentHuggingPriority(.defaultHigh, for: .horizontal)
            NSLayoutConstraint.activate([label.widthAnchor.constraint(greaterThanOrEqualToConstant: 100)])
            h.addArrangedSubview(label)
            field.setContentHuggingPriority(.defaultLow, for: .horizontal)
            NSLayoutConstraint.activate([field.widthAnchor.constraint(greaterThanOrEqualToConstant: 200)])
            h.addArrangedSubview(field)
            return h
        }
    stack.addArrangedSubview(labeled("Host", hostField))
    stack.addArrangedSubview(labeled("Porta", portField))
    stack.addArrangedSubview(labeled("Intervalo (s)", refreshField))
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

    func postNotification(title: String, body: String) {
        guard canUseUserNotifications() else {
            print("[Notifications] Skipped (not in .app bundle): \(title) - \(body)")
            return
        }
        let content = UNMutableNotificationContent()
        content.title = title
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
        let payload: [String: Any] = [
            "chat_id": s.telegramChatId,
            "text": body
        ]
        req.httpBody = try? JSONSerialization.data(withJSONObject: payload, options: [])
        print("[Telegram] Sending to chat \(s.telegramChatId): \(body)")
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
        // Fallback: open System Settings app
        NSWorkspace.shared.launchApplication("System Settings")
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
    var host: String = "127.0.0.1"
    var port: Int = 3551
    var refreshSeconds: Int = 10
    var telegramEnabled: Bool = false
    var telegramBotToken: String = ""
    var telegramChatId: String = ""
    // Monitoring thresholds (defaults typical for 220V / 60Hz region, adjust as needed)
    var voltageHigh: Double = 255.0
    var voltageLow: Double = 180.0
    var frequencyLow: Double = 55.0
    var frequencyHigh: Double = 65.0
    var voltageAlertsEnabled: Bool = true

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
                        Text(statusData["STATUS"] ?? "Desconhecido")
                            .font(.subheadline)
                            .foregroundColor(.secondary)
                    }
                    Spacer()
                    Button("Atualizar") { refresh() }
                }
                .padding()
                .background(cardBackground)
                .cornerRadius(8)
                
                // Battery & Load
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
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }

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

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.run()
