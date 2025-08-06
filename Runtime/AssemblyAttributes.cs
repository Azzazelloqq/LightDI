using System.Runtime.CompilerServices;

// Allow test assembly to access internal types
[assembly: InternalsVisibleTo("LightDI.Tests")]

// This file ensures that the test assembly can access internal types
// from the Core assembly for testing purposes.