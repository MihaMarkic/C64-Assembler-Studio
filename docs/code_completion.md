# C-64 Assembler Studio
## Code completion

Application parses code as it changes and is capable of providing code completion suggestions.
Code completion suggestions starts implicitly (when typing one of " # . , = chars) or when explicitly requested though CTRL+SPACE.

 Code completion helps with:
 * completing keywords, label names, enum values, function names, variable names and more
 * file completion
 * directive arguments completion
 
 Samples (**|** denotes cursor position when code commpletion is invoked):

 ```
 .function Test(a) { }

 T|
 ```

 would show code completion options: `tan, tanh, Test, toDegrees and toRadians` where Test is part of code and others are from `Math` library.

 ```
 ##import c64 "|
 ```
would should available file and directory names relative to the project or libraries used. When selecting a file it would also and double quotes. When selecting a directory, type / and invoke code completion again.