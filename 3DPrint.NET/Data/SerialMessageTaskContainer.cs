namespace _3DPrint.NET.Data;

internal record SerialMessageTaskContainer<T, Tcode>(Func<T, Task> Func, Tcode SerialCode) where Tcode : Enum;