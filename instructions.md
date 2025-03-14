Here's your updated prompt with layers added as a future improvement:

# MS Paint Clone Application Prompt
- always keep in mind that application must be long term maintainable - so keep code clean

## Project Overview
1. Create a cross-platform MS Paint clone with the nostalgic feel of the original Windows application, using .NET for compatibility across Windows and Mac.

## Core Features
2. Simple, intuitive UI reminiscent of classic MS Paint
3. Basic drawing tools (pencil, brush, eraser, shapes)
4. Color palette with transparency support
5. Image manipulation basics (copy, paste, selection, movement)
6. Cross-platform compatibility (Windows and Mac)
7. Zoom functionality to scale the canvas view (CTRL++ to zoom in, CTRL+- to zoom out, CTRL+mouse wheel for smooth incremental zoom)
8. Optional grid overlay for precise alignment (toggle with CTRL+G)

## Technical Requirements
9. Build with .NET MAUI (Multi-platform App UI) or .NET 8 with Avalonia UI
10. Implement a canvas-based drawing system using SkiaSharp
11. Support standard image formats (PNG, JPEG, BMP)
12. Include transparency handling in the color picker
13. Ensure minimal system requirements

## User Interface Elements
14. Toolbar with essential drawing tools
15. Simple color palette with transparency slider
16. Status bar showing cursor position and canvas size
17. Familiar menu structure (File, Edit, View, Image, Help)

## Drawing Tools
18. Pencil tool for pixel-precise drawing (P)
19. Brush tool with configurable size (B)
20. Eraser with adjustable size (E)
21. Basic shapes (rectangle (R), ellipse (O), line (L), polygon (Y))
22. Text insertion tool (T)

## Selection and Manipulation
23. Rectangular (S) and free-form (F) selection tools
24. Cut (CTRL+X), copy (CTRL+C), and paste (CTRL+V) functionality
25. Move selection with mouse drag or keyboard arrow keys (pixel-precise movement)
26. Rotation (CTRL+R) and resize (CTRL+W) options for selections

## File Operations
27. New document creation (CTRL+N) with custom dimensions
28. Open existing images (CTRL+O) in various formats
29. Save (CTRL+S) and Save As (CTRL+SHIFT+S) in multiple formats including transparency-supporting formats
30. Print functionality (CTRL+P)
31. Document resize functionality accessible via CTRL+E shortcut

## .NET-Specific Implementation
32. Use XAML for UI layout
33. Leverage SkiaSharp for drawing operations
34. Implement INotifyPropertyChanged for UI updates
35. Use dependency injection for better code organization
36. Ensure accessibility features work across platforms

## Future Improvements
37. Layer support with visibility toggles and blend modes (low priority due to requiring custom file format development)