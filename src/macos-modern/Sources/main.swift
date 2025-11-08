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

    func fetchStatus(timeout: TimeInterval = 2.0) -> (UpsState, [String:String]) {
        var dict: [String:String] = [:]
        var state: UpsState = .commlost

        // NIS: Connect and send "status\n"
        let semaphore = DispatchSemaphore(value: 0)
        DispatchQueue.global().async {
            defer { semaphore.signal() }
            guard let streamPair = self.openStream() else { return }
            let (input, output) = streamPair
            let cmd = "status\n"
            output.write(cmd.data(using: .ascii)!, maxLength: cmd.count)
            output.close() // server pushes status then closes

            let bufferSize = 4096
            let buffer = UnsafeMutablePointer<UInt8>.allocate(capacity: bufferSize)
            defer { buffer.deallocate() }
            var data = Data()
            while input.hasBytesAvailable {
                let read = input.read(buffer, maxLength: bufferSize)
                if read <= 0 { break }
                data.append(buffer, count: read)
            }
            input.close()

            if let text = String(data: data, encoding: .ascii) {
                for line in text.split(separator: "\n") {
                    let parts = line.split(separator: ":", maxSplits: 1)
                    if parts.count == 2 {
                        let key = parts[0].trimmingCharacters(in: .whitespaces)
                        let value = parts[1].trimmingCharacters(in: .whitespaces)
                        dict[key] = value
                    }
                }
                // Determine state heuristics using NIS fields
                if dict["STATUS"]?.contains("COMMLOST") == true { state = .commlost }
                else if dict["STATUS"]?.contains("ONBATT") == true { state = .onbatt }
                else if dict["BCHARGE"].flatMap({ Double($0.split(separator: " ").first ?? "0") }) ?? 100 < 100 { state = .charging }
                else { state = .online }
            }
        }
        _ = semaphore.wait(timeout: .now() + timeout)
        return (state, dict)
    }

    func fetchEvents(timeout: TimeInterval = 2.0) -> [String] {
        var events: [String] = []
        let semaphore = DispatchSemaphore(value: 0)
        DispatchQueue.global().async {
            defer { semaphore.signal() }
            guard let (input, output) = self.openStream() else { return }
            let cmd = "events\n"
            output.write(cmd.data(using: .ascii)!, maxLength: cmd.count)
            output.close()

            let bufferSize = 4096
            let buffer = UnsafeMutablePointer<UInt8>.allocate(capacity: bufferSize)
            defer { buffer.deallocate() }
            var data = Data()
            while input.hasBytesAvailable {
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
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
    var client: NisClient!
    var timer: Timer?
    var lastEventLine: String = ""
    var eventsCache: [String] = []
    var eventsWindow: EventsWindowController?
    var statusWindow: NSWindow?

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let button = statusItem.button { button.image = makeIcon(system: "exclamationmark.circle") }
        // Carregar configurações persistidas
        Settings.shared.load()
        client = NisClient(host: Settings.shared.host, port: Settings.shared.port)
        constructMenu()
        // Solicitar permissão para notificações
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound, .badge]) { _, _ in }
        poll()
        startTimer()
    }
    func startTimer() {
        timer?.invalidate()
        let interval = max(2.0, TimeInterval(Settings.shared.refreshSeconds))
        timer = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { _ in self.poll() }
    }

    func constructMenu() {
        let menu = NSMenu()
        menu.addItem(NSMenuItem(title: "Status", action: #selector(showStatus), keyEquivalent: "s"))
        menu.addItem(NSMenuItem(title: "Eventos", action: #selector(showEvents), keyEquivalent: "e"))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Configuração", action: #selector(showConfig), keyEquivalent: "c"))
        menu.addItem(NSMenuItem(title: "Autoteste (em breve)", action: #selector(runSelfTestPlaceholder), keyEquivalent: "t"))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Sair", action: #selector(quit), keyEquivalent: "q"))
        statusItem.menu = menu
    }

    @objc func poll() {
        let (state, dict) = client.fetchStatus()
        if let button = statusItem.button {
            switch state {
            case .commlost: button.image = makeIcon(system: "bolt.slash")
            case .onbatt:   button.image = makeIcon(system: "bolt.fill")
            case .charging: button.image = makeIcon(system: "battery.50")
            case .online:   button.image = makeIcon(system: "checkmark.circle")
            }
            let statusText = dict["STATUS"] ?? "Desconhecido"
            button.toolTip = "UPS: \(dict["UPSNAME"] ?? "?")\nStatus: \(statusText)"
        }

        // Buscar eventos para notificar novidades
        let events = client.fetchEvents()
        eventsCache = events
        if let last = events.last, last != lastEventLine {
            lastEventLine = last
            postNotification(title: "APC UPS", body: last)
        }
        eventsWindow?.update(with: eventsCache)
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
        alert.informativeText = "Editar parâmetros de conexão e intervalo."
        alert.addButton(withTitle: "Salvar")
        alert.addButton(withTitle: "Cancelar")

        let hostField = NSTextField(string: Settings.shared.host)
        hostField.placeholderString = "Host"
        let portField = NSTextField(string: String(Settings.shared.port))
        portField.placeholderString = "Porta"
        let refreshField = NSTextField(string: String(Settings.shared.refreshSeconds))
        refreshField.placeholderString = "Intervalo (s)"

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
            h.addArrangedSubview(label)
            field.frame.size.width = 160
            h.addArrangedSubview(field)
            return h
        }
        stack.addArrangedSubview(labeled("Host", hostField))
        stack.addArrangedSubview(labeled("Porta", portField))
        stack.addArrangedSubview(labeled("Intervalo (s)", refreshField))
        stack.translatesAutoresizingMaskIntoConstraints = false
        stack.setFrameSize(NSSize(width: 260, height: 90))

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
            Settings.shared.save()
            // Recriar cliente e timer
            client = NisClient(host: Settings.shared.host, port: Settings.shared.port)
            startTimer()
            simpleAlert(title: "Salvo", message: "Host: \(validHost) Porta: \(validPort) Intervalo: \(validRefresh)s")
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
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = body
        let request = UNNotificationRequest(identifier: UUID().uuidString, content: content, trigger: nil)
        UNUserNotificationCenter.current().add(request, withCompletionHandler: nil)
    }
}

// MARK: - Settings persistence
final class Settings {
    static let shared = Settings()
    private init() {}
    private let kHost = "nisHost"
    private let kPort = "nisPort"
    private let kRefresh = "nisRefresh"
    var host: String = "127.0.0.1"
    var port: Int = 3551
    var refreshSeconds: Int = 10

    func load() {
        let d = UserDefaults.standard
        if let h = d.string(forKey: kHost) { host = h }
        let p = d.integer(forKey: kPort); if p != 0 { port = p }
        let r = d.integer(forKey: kRefresh); if r != 0 { refreshSeconds = r }
    }
    func save() {
        let d = UserDefaults.standard
        d.set(host, forKey: kHost)
        d.set(port, forKey: kPort)
        d.set(refreshSeconds, forKey: kRefresh)
    }
}

// MARK: - Status View (SwiftUI)
struct StatusView: View {
    let fetchStatus: () -> [String: String]
    @State private var statusData: [String: String] = [:]
    @State private var timer: Timer?
    
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
                .background(Color(nsColor: .controlBackgroundColor))
                .cornerRadius(8)
                
                // Battery & Load
                HStack(spacing: 16) {
                    MetricCard(
                        title: "Bateria",
                        value: statusData["BCHARGE"] ?? "--",
                        icon: "battery.75"
                    )
                    MetricCard(
                        title: "Carga",
                        value: statusData["LOADPCT"] ?? "--",
                        icon: "gauge"
                    )
                }
                
                // Voltage & Frequency
                HStack(spacing: 16) {
                    MetricCard(
                        title: "Entrada",
                        value: statusData["LINEV"] ?? "--",
                        icon: "arrow.down.circle"
                    )
                    MetricCard(
                        title: "Saída",
                        value: statusData["OUTPUTV"] ?? "--",
                        icon: "arrow.up.circle"
                    )
                }
                
                HStack(spacing: 16) {
                    MetricCard(
                        title: "Frequência",
                        value: statusData["LINEFREQ"] ?? "--",
                        icon: "waveform"
                    )
                    MetricCard(
                        title: "Tempo Rest.",
                        value: statusData["TIMELEFT"] ?? "--",
                        icon: "clock"
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
        .onAppear { refresh() }
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
}

struct MetricCard: View {
    let title: String
    let value: String
    let icon: String
    
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
        .background(Color(nsColor: .controlBackgroundColor))
        .cornerRadius(6)
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

        let scroll = NSScrollView(frame: window.contentView?.bounds ?? rect)
        scroll.autoresizingMask = [.width, .height]
        scroll.hasVerticalScroller = true
        scroll.hasHorizontalScroller = true

        textView.isEditable = false
        if #available(macOS 13.0, *) {
            textView.font = .monospacedSystemFont(ofSize: 12, weight: .regular)
        } else {
            textView.font = .systemFont(ofSize: 12)
        }
        textView.textContainerInset = NSSize(width: 6, height: 6)
        scroll.documentView = textView
        window.contentView?.addSubview(scroll)
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    func update(with lines: [String]) {
        textView.string = lines.joined(separator: "\n")
        textView.scrollToEndOfDocument(nil)
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.run()
