# B64Converter

Small CLI that scans a .NET EXE or DLL file and replaces Base64-encoded strings decoded at runtime with their readable text versions.

Example:
```
Encoding.UTF8.GetString(Convert.FromBase64String(“U09GVFdBUkVcTWljcm9zb2Z0XFdpbmRvd3MgTlRcQ3VycmVudFZlcnNpb24=”))
```
is converted to:
```
“SOFTWARE\Microsoft\Windows NT\CurrentVersion”
```

The modified file is saved as `<original-name>-converted.exe`.

---

## Example
.Net packed with VMProtect and unpacked

Before

<img width="1140" height="450" alt="image" src="https://github.com/user-attachments/assets/b8fce0d1-6eab-4e61-9a6b-c2b25e033f1a" />

After

<img width="676" height="470" alt="image" src="https://github.com/user-attachments/assets/7301d2b0-1b71-4ab3-a51a-12b8989a7402" />

## Requirements
- .NET Framework 4.8  
- NuGet package: `dnlib`

---

## What it does
- Detects and decodes patterns such as:
  - `Encoding.UTF8.GetString(Convert.FromBase64String(“...”)`
  - `Encoding.ASCII.GetString(Convert.FromBase64String(“...”)`
  - `Encoding.Unicode.GetString(Convert.FromBase64String(“...”)`
- Replaces them directly in IL with the decoded text string.
- It skips anything that does not decode into readable ASCII/UTF8 text.

---

## Notes
- It tries UTF-8 decoding first, then falls back to ASCII.
- It uses a simple printable character check to avoid replacing binary data.
- It overwrites the original EXE instead of saving `-converted`.
