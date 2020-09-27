# CecilStrip
Uses [Mono.Cecil](https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/) to strip ECMA CIL libraries (e.g. .NET and Mono DLLs) of all code, keeping only type signatures.

## Usage
### Single file
```batch
CecilStrip.exe "path\to\a\file.dll"
```

### Multiple files
```batch
CecilStrip.exe "path\to\a\file.dll" "C:\path\to\another\file.dll"
```

### Multiple files and an output folder
```batch
CecilStrip.exe -o "some\output\folder" "path\to\a\file.dll" "path\to\another\file.dll"
```

### Increase verbosity
```batch
CecilStrip.exe -v "path\to\a\file.dll"
```
