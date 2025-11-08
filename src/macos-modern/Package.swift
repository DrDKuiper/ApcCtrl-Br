// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "apcctrl-macos-modern",
    platforms: [.macOS(.v12)],
    products: [
        .executable(name: "apcctrl-macos-modern", targets: ["App"])
    ],
    targets: [
        .executableTarget(name: "App", path: "Sources")
    ]
)
