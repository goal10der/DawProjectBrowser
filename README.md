# DawProjectBrowser

DawProjectBrowser is a cross-platform desktop application for browsing music production projects and previewing their audio without opening a DAW. It scans folders for supported DAW project files, associates them with recent demo audio, and presents them in a clean, searchable interface.

The application runs on Windows, macOS, and Linux.

## Features

### **Project Discovery**

- Recursively scans a user-selected folder
- Detects supported DAW project files:
- Logic Pro (.logicx)
- FL Studio (.flp)
- Ableton Live (.als)
- Automatically ignores common non-project folders:
- backup, backups
- .git, .svn
- render

## Audio Preview

- Automatically finds the most recent audio file in each project folder
- Supported audio formats:
- WAV
- MP3
- FLAC
- M4A
- Playback controls:
- Play
- Pause
- Resume
- Stop
- Scrub to position
- Playback position updates in real time
- Only one project plays at a time

## Visual Identification

- Projects are labeled with DAW-specific logos
- Logos can be overridden by the user
- External logo files take priority over built-in assets

## Theme Customization

- Supports runtime theme loading using Avalonia .axaml files
- Themes can be added or removed without recompiling
- The app can revert to the default theme at any time
