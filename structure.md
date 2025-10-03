# Program Structure

## Entry Point & Orchestration

**Program.cs** - Main entry point, creates all dependencies, runs the main event loop
- Gets master password
- Creates instances of UI, storage, and managers
- Main loop: refreshes display every second, handles keyboard input
- Routes commands to appropriate classes

## UI Layer

**ConsoleUI.cs** - All console display logic
- Password input (masked)
- Main screen display with account list
- TOTP code display with countdown timer
- Screen management (clear, refresh, messages)

## Business Logic

**AccountManager.cs** - Account operations (depends on AccountStorage)
- Add account with validation
- Remove account with confirmation

**ClipboardHelper.cs** - Cross-platform clipboard support
- Copies TOTP codes to clipboard
- Detects OS and uses appropriate command (clip/pbcopy/xclip)

## Data & Crypto Layer

**AccountStorage.cs** - Encrypted persistence
- Saves/loads accounts as encrypted JSON
- Uses AES encryption via CryptoHelper

**TotpGenerator.cs** - TOTP algorithm (RFC 6238)
- Generates 6-digit codes
- Base32 decoding
- 30-second time windows

**CryptoHelper.cs** - Encryption utilities
- AES with PBKDF2 key derivation

**OtpAccount.cs** - Simple data model (Name + Secret)

## Data Flow

```
User Input → Program → AccountManager/ConsoleUI/ClipboardHelper
                ↓
          AccountStorage → CryptoHelper → File System
                ↓
          TotpGenerator → Display Codes
```

## Architecture Pattern

The application uses **dependency injection** with instance-based classes:
- `AccountManager` receives `AccountStorage` via constructor
- `Program.cs` wires up all dependencies
- No static classes (except Program entry point)
- Better testability and separation of concerns
