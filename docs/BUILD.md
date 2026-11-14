# Build the Windows installer

The release ZIP contains the installer. This source directory exposes the installer logic for review and recompilation; it does not include original game archives, source English catalogs, development decoders or a bulk extraction toolchain.

On Windows with .NET Framework 4.8, obtain the official 1.0.0 installer from the release ZIP, then run from PowerShell:

~~~powershell
.\source\BUILD.ps1 -InstallerPath C:\Downloads\UBW-Thai-Setup.exe -OutputPath .\.build\UBW-Thai-Setup.exe -SelfTest
~~~

The script verifies the release installer's SHA256, reads its embedded localization/font resources without running its entry point, and compiles the maintained C# sources with the Windows .NET Framework compiler. It needs no network, Node, Python or installed game. It refuses to overwrite an existing output. Source/resources/behaviour are reproducible; the PE binary hash may differ due to compiler timestamps.

This is a way to rebuild the installer around the released payload. It is not the full translation/font-generation pipeline. Translation overrides and all-source development validation remain in the maintainer's separate workspace. Self-tests use isolated temporary fixtures and never install into the real game.

The public installer enforces the real game's closed-process/build/hash guards. Only the embedded self-test injects a fixture-only policy for paths under its randomly named temporary test directory.
