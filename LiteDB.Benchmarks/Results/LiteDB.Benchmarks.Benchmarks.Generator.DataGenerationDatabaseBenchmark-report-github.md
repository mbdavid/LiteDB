``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.4 (18E226) [Darwin 18.5.0]
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
  [Host] : Mono 5.18.1.3 (2018-08/fdb26b0a445 Wed), 64bit 
  Core   : .NET Core 2.2.2 (CoreCLR 4.6.27317.07, CoreFX 4.6.27318.02), 64bit RyuJIT
  Mono   : Mono 5.18.1.3 (2018-08/fdb26b0a445 Wed), 64bit 

Force=True  

```
|                       Method |  Job |    Jit | Runtime |         Arguments |     Toolchain |     N |          Mean |         Error |        StdDev |        Median |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|----------------------------- |----- |------- |-------- |------------------ |-------------- |------ |--------------:|--------------:|--------------:|--------------:|----------:|---------:|---------:|----------:|
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** |    **10** |      **18.71 us** |     **0.2287 us** |     **0.2139 us** |      **18.61 us** |    **1.5259** |        **-** |        **-** |    **4856 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 |    10 |      18.78 us |     0.2493 us |     0.2210 us |      18.71 us |    1.5259 |        - |        - |    4856 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |    10 |   1,085.13 us |    24.9952 us |    70.9073 us |   1,059.70 us |         - |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |    10 |   1,075.49 us |    24.0434 us |    68.9851 us |   1,049.30 us |         - |        - |        - |         - |
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** |    **50** |      **86.98 us** |     **0.4309 us** |     **0.3820 us** |      **86.95 us** |    **6.9580** |        **-** |        **-** |   **22152 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 |    50 |      88.96 us |     1.2176 us |     1.0794 us |      88.49 us |    6.9580 |        - |        - |   22152 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |    50 |   4,656.46 us |    92.2635 us |   156.6704 us |   4,631.50 us |         - |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |    50 |   4,713.79 us |   130.1759 us |   121.7667 us |   4,681.60 us |         - |        - |        - |         - |
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** |   **100** |     **168.24 us** |     **0.7451 us** |     **0.6970 us** |     **168.29 us** |   **13.9160** |        **-** |        **-** |   **44240 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 |   100 |     166.00 us |     0.6268 us |     0.5863 us |     166.01 us |   13.9160 |        - |        - |   44240 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |   100 |   8,784.15 us |   174.2213 us |   281.3349 us |   8,813.10 us |         - |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |   100 |   8,784.89 us |   174.0031 us |   243.9280 us |   8,803.30 us |         - |        - |        - |         - |
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** |   **500** |     **844.45 us** |     **3.9763 us** |     **3.5248 us** |     **846.01 us** |   **57.6172** |  **21.4844** |        **-** |  **217952 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 |   500 |     824.30 us |     4.0973 us |     3.6322 us |     824.28 us |   57.6172 |  21.4844 |        - |  217952 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |   500 |  40,837.19 us |   775.8730 us |   796.7644 us |  40,857.10 us |         - |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |   500 |  40,677.30 us |   799.6876 us |   821.2203 us |  40,860.50 us |         - |        - |        - |         - |
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** |  **1000** |   **1,674.49 us** |     **5.0133 us** |     **4.4442 us** |   **1,674.88 us** |  **128.9063** |  **64.4531** |        **-** |  **437528 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 |  1000 |   1,727.45 us |     7.0267 us |     6.2290 us |   1,728.30 us |  128.9063 |  64.4531 |        - |  437528 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |  1000 |  79,503.33 us | 1,467.6204 us | 1,372.8131 us |  79,617.90 us |         - |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |  1000 |  79,768.65 us | 1,092.4973 us | 1,021.9227 us |  79,936.00 us |         - |        - |        - |         - |
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** |  **5000** |  **10,067.94 us** |    **69.8066 us** |    **58.2917 us** |  **10,090.73 us** |  **546.8750** | **250.0000** |  **62.5000** | **2237088 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 |  5000 |  10,373.69 us |    79.2451 us |    74.1259 us |  10,390.97 us |  546.8750 | 250.0000 |  62.5000 | 2237088 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |  5000 | 386,649.52 us | 3,814.4469 us | 3,185.2359 us | 386,395.00 us |         - |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default |  5000 | 384,825.91 us | 2,295.3779 us | 2,034.7927 us | 384,935.75 us |         - |        - |        - |         - |
|               **DataGeneration** | **Core** | **RyuJit** |    **Core** |           **Default** | **.NET Core 2.2** | **10000** |  **26,028.40 us** |   **494.6715 us** |   **485.8336 us** |  **25,864.08 us** | **1031.2500** | **406.2500** | **156.2500** | **4470016 B** |
| DataWithExclusionsGeneration | Core | RyuJit |    Core |           Default | .NET Core 2.2 | 10000 |  26,336.93 us |   492.5689 us |   436.6495 us |  26,332.96 us | 1000.0000 | 406.2500 | 156.2500 | 4469818 B |
|               DataGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default | 10000 | 779,342.73 us | 3,558.5193 us | 3,328.6412 us | 778,185.50 us | 1000.0000 |        - |        - |         - |
| DataWithExclusionsGeneration | Mono |   Llvm |    Mono | --optimize=inline |       Default | 10000 | 776,147.37 us | 2,991.0797 us | 2,797.8578 us | 776,135.50 us | 1000.0000 |        - |        - |         - |
