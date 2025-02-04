# Project SFloat

SFloat is a .NET library that provides a way to represent and perform calculations
of floating-point numbers based on string manipulation.
Because of its string-based nature, SFloat can represent floating-point numbers with
theoretically infinite precision. Actual precision is limited by the available memory
and the actual `int` size since it involves index access to the string.

SFloat is purely an experimental project for fun
and it does absolutely not consider performance.

## Usage

```csharp
var a = new SFloat("13.498");      // Creates a decimal SFloat with value 13.498
var b = new SFloat("A3.B1", 16);   // Creates a hexadecimal SFloat with value 163.69140625
Console.WriteLine(a * b);          // Multiply a and b, convert the result to the same base as a.
/* OUTPUT: 
 *    2209.5066015625
 */
```

## Features

- Basic arithmetic operations (division is not currently implemented).
- Supports any number bases from 2 to 36.
- Supports conversion between supported bases.

## Suggested Use Scenarios

I can't really think of any. I just made this when I was bored. XD

(Or, maybe, you can use this when you want to calculate with a very high precision.)