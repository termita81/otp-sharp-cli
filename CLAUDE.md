# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# console application for generating Time-based One-Time Passwords (TOTP). It provides a CLI interface for managing OTP accounts with encrypted storage of secrets.

## Development Commands

### Build and Run
```bash
dotnet build
dotnet run [database-file]
```

### Development Build
```bash
dotnet build --configuration Debug
```

### Release Build
```bash
dotnet build --configuration Release
```

## Architecture

The application follows a simple modular design with clear separation of concerns:

- **Program.cs**: Main console UI and user interaction logic
- **OtpAccount.cs**: Simple data model for account storage
- **TotpGenerator.cs**: TOTP algorithm implementation with Base32 decoding
- **AccountStorage.cs**: Account persistence with encryption/decryption
- **CryptoHelper.cs**: AES encryption using PBKDF2 for key derivation

### Key Components

**Storage Architecture**: Accounts are encrypted using AES with PBKDF2-derived keys and stored as JSON. The encryption parameters (salt, IV, iterations) ensure secure storage of TOTP secrets.

**TOTP Implementation**: Custom implementation of RFC 6238 TOTP algorithm with 30-second time steps and 6-digit codes. Uses HMAC-SHA1 and proper Base32 decoding.

**Console Interface**: Interactive menu-driven interface with secure password input (masked typing) and real-time code display with countdown timer.

## Project Configuration

- **Target Framework**: .NET 10.0
- **Project Type**: Console application
- **Namespace**: `OtpSharp`
- **Features**: Nullable reference types enabled, implicit usings enabled

## File Structure

The codebase uses a flat structure with all source files in the root directory. The database file (default: `otp-accounts.json`) is created in the user's home directory or specified via command line argument.