# Program Structure

## Entry Point

**Program.cs** - Simple entry point (34 lines)
- Displays welcome message
- Gets database file path from args or default location
- Creates UI and gets master password
- Creates AccountStorage and OtpApplication
- Handles top-level exceptions

## Application Orchestration

**OtpApplication.cs** - Main application coordinator (183 lines)
- Encapsulates all dependencies (ConsoleUI, AccountManager, AccountStorage, ClipboardHelper)
- Manages application state (visible code index, timers)
- Main event loop: refreshes display, handles keyboard input
- Coordinates workflows between UI and logic layers
- Methods: `Run()`, `HandleAddAccount()`, `HandleRemoveAccount()`, `HandleCopyCode()`, `HandleNumberKey()`

## UI Layer

**ConsoleUI.cs** - All user interaction (236 lines)
- Password input (masked)
- Main screen display with account list
- TOTP code display with countdown timer
- Screen management (clear, refresh, messages)
- User input collection (`GetAccountInput()`, `SelectAccountToRemove()`)
- User confirmations (`ConfirmTestCode()`, `ConfirmRemoval()`)
- Consistent message display (`ShowMessage()`, `WaitForKey()`)

## Business Logic Layer

**AccountManager.cs** - Pure business logic (64 lines)
- No Console I/O - returns values/booleans
- Account operations: `AddAccount()`, `RemoveAccount()`, `GetAccounts()`
- Secret validation: `ValidateSecret()`, `GenerateTestCode()`
- Depends on AccountStorage via constructor

**ClipboardHelper.cs** - Cross-platform clipboard support
- Copies TOTP codes to clipboard
- Detects OS and uses appropriate command (clip/pbcopy/xclip/xsel)
- Returns boolean for success/failure

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
User Input → ConsoleUI → OtpApplication → AccountManager → AccountStorage
                                ↓                              ↓
                          ClipboardHelper              CryptoHelper
                                ↓                              ↓
                          TotpGenerator                  File System
                                ↓
                          Display Codes
```

## Workflow Examples

### Adding an Account
1. User presses 'a'
2. `OtpApplication.HandleAddAccount()` called
3. `ConsoleUI.GetAccountInput()` collects name and secret
4. `AccountManager.ValidateSecret()` checks format
5. `AccountManager.GenerateTestCode()` creates test code
6. `ConsoleUI.ConfirmTestCode()` asks user to verify
7. `AccountManager.AddAccount()` saves to storage
8. `ConsoleUI.ShowMessage()` displays result

### Removing an Account
1. User presses 'd'
2. `OtpApplication.HandleRemoveAccount()` called
3. `AccountManager.GetAccounts()` retrieves list
4. `ConsoleUI.SelectAccountToRemove()` displays list and gets selection
5. `ConsoleUI.ConfirmRemoval()` asks for confirmation
6. `AccountManager.RemoveAccount()` deletes from storage
7. `ConsoleUI.ShowMessage()` displays result

## Architecture Pattern

The application uses **clean separation of concerns** with dependency injection:

**Layers:**
- **Entry**: Program.cs creates and wires dependencies
- **Orchestration**: OtpApplication coordinates workflows and manages state
- **UI**: ConsoleUI handles all user interaction (input/output)
- **Logic**: AccountManager provides pure business logic
- **Data**: AccountStorage, TotpGenerator, CryptoHelper handle persistence and algorithms

**Benefits:**
- Clear responsibilities - no mixed concerns
- Easy to test - logic separated from I/O
- Easy to modify - swap UI without changing logic
- Consistent patterns - all operations follow same flow
- No parameter sprawl - dependencies injected via constructors
