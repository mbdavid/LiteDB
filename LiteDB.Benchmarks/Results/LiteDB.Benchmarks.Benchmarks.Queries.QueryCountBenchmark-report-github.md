``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.4 (18E226) [Darwin 18.5.0]
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
  [Host] : Mono 5.16.1.0 (2018-06/a76b50e5faa Thu), 64bit 

Force=True  

```
|              Method |  Job |    Jit | Runtime |         Arguments |     Toolchain |  FileMode |       Password |     N | IsJournalEnabled | Mean | Error | Ratio | RatioSD |
|-------------------- |----- |------- |-------- |------------------ |-------------- |---------- |--------------- |------ |----------------- |-----:|------:|------:|--------:|
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |    **10** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    10 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    10 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    10 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    10 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    10 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |    **10** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    10 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    10 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    10 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    10 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    10 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |    **50** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    50 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    50 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    50 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    50 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    50 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |    **50** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    50 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |    50 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    50 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    50 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |    50 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |   **100** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   100 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   100 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   100 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   100 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   100 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |   **100** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   100 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   100 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   100 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   100 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   100 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |   **500** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   500 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   500 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   500 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   500 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   500 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |   **500** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   500 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |   500 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   500 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   500 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |   500 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |  **1000** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  1000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  1000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  1000 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  1000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  1000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |  **1000** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  1000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  1000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  1000 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  1000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  1000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |  **5000** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  5000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  5000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  5000 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  5000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  5000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** |  **5000** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  5000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? |  5000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  5000 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  5000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? |  5000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** | **10000** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? | 10000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? | 10000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? | 10000 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? | 10000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? | 10000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** |              **?** | **10000** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? | 10000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive |              ? | 10000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? | 10000 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? | 10000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive |              ? | 10000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |    **10** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    10 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    10 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    10 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    10 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    10 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |    **10** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    10 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    10 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    10 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    10 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    10 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |    **50** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    50 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    50 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    50 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    50 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    50 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |    **50** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    50 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |    50 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    50 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    50 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |    50 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |   **100** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   100 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   100 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   100 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   100 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   100 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |   **100** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   100 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   100 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   100 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   100 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   100 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |   **500** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   500 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   500 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   500 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   500 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   500 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |   **500** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   500 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |   500 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   500 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   500 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |   500 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |  **1000** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  1000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  1000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  1000 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  1000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  1000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |  **1000** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  1000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  1000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  1000 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  1000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  1000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |  **5000** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  5000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  5000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  5000 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  5000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  5000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** |  **5000** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  5000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword |  5000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  5000 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  5000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword |  5000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** | **10000** |            **False** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword | 10000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword | 10000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword | 10000 |            False |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword | 10000 |            False |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword | 10000 |            False |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       **CountWithLinq** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.1** | **Exclusive** | **SecurePassword** | **10000** |             **True** |   **NA** |    **NA** |     **?** |       **?** |
| CountWithExpression | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword | 10000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Core | RyuJit |    Core |           Default | .NET Core 2.1 | Exclusive | SecurePassword | 10000 |             True |   NA |    NA |     ? |       ? |
|                     |      |        |         |                   |               |           |                |       |                  |      |       |       |         |
|       CountWithLinq | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword | 10000 |             True |   NA |    NA |     ? |       ? |
| CountWithExpression | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword | 10000 |             True |   NA |    NA |     ? |       ? |
|      CountWithQuery | Mono |   Llvm |    Mono | --optimize=inline |       Default | Exclusive | SecurePassword | 10000 |             True |   NA |    NA |     ? |       ? |

Benchmarks with issues:
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=?, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=50, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=100, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=500, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=1000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=5000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=False]
  QueryCountBenchmark.CountWithLinq: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Core(Jit=RyuJit, Runtime=Core, Force=True, Toolchain=.NET Core 2.1) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithLinq: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithExpression: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=True]
  QueryCountBenchmark.CountWithQuery: Mono(Jit=Llvm, Runtime=Mono, Force=True, Arguments=--optimize=inline) [FileMode=Exclusive, Password=SecurePassword, N=10000, IsJournalEnabled=True]
