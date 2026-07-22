# Automation1 SDK runtime files

These files are copied from `etc/downloadable/a1참고자료` so the downloadable WPF sample can build without a repository-relative dependency.

| File | Purpose |
| --- | --- |
| `Aerotech.Automation1.DotNet.dll` | Official strongly typed Automation1 .NET API, version 2.13.1.3468 |
| `Aerotech.Automation1.Communication.dll` | Automation1 communication dependency |
| `Aerotech.Automation1.DotNet.xml` | API XML documentation used during implementation and IDE help |
| `Automation1Compiler.dll` | AeroScript compiler runtime dependency |
| `Automation1Compiler64.dll` | x64 AeroScript compiler runtime dependency |

The project builds as x64. Keep these files aligned with the Automation1 MDK/Runtime version installed on the target Client and Server PCs.
