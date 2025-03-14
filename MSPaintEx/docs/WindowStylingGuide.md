# Avalonia Window Styling Guide - MSPaintEx Style

## Basic Window Setup
```xaml
<Window
    Width="500"
    Height="400"
    WindowStartupLocation="CenterOwner"
    CanResize="False"
    ExtendClientAreaToDecorationsHint="True"
    ExtendClientAreaChromeHints="NoChrome"
    ExtendClientAreaTitleBarHeightHint="-1"
    Background="#881E90FF"
    TransparencyLevelHint="AcrylicBlur">
```

### Core Window Properties
- `WindowStartupLocation="CenterOwner"` - Centers window relative to parent
- `CanResize="False"` - Prevents window resizing
- `ExtendClientAreaToDecorationsHint="True"` - Enables custom chrome
- `ExtendClientAreaChromeHints="NoChrome"` - Removes default window chrome
- `ExtendClientAreaTitleBarHeightHint="-1"` - Custom title bar height

## Background Layers
The window uses a 3-layer background system for depth:

### Layer 1: Window Background
```xaml
Background="#881E90FF"
```
- Base color: Dodger Blue (`#1E90FF`)
- Alpha: 53% (`88`)
- Purpose: Sets the main window tint

### Layer 2: Acrylic Effect
```xaml
<ExperimentalAcrylicBorder IsHitTestVisible="False">
    <ExperimentalAcrylicBorder.Material>
        <ExperimentalAcrylicMaterial
            BackgroundSource="Digger"
            TintColor="#1E90FF"
            TintOpacity="0.5"
            MaterialOpacity="0.2"/>
    </ExperimentalAcrylicBorder.Material>
</ExperimentalAcrylicBorder>
```
- `TintColor`: Matches window background
- `TintOpacity`: 50% for subtle effect
- `MaterialOpacity`: 20% for minimal blur
- Purpose: Creates glass-like effect

### Layer 3: Contrast Gradient
```xaml
<Panel>
    <Panel.Background>
        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
            <GradientStop Offset="0" Color="#15104080"/>
            <GradientStop Offset="1" Color="#25104080"/>
        </LinearGradientBrush>
    </Panel.Background>
</Panel>
```
- Diagonal gradient: Top-left to bottom-right
- Colors: Semi-transparent blue (`104080`)
- Alpha: 8% to 14% (`15` to `25`)
- Purpose: Adds subtle depth

## Typography Guidelines

### Text Colors
- Primary Text: `#000000` (100% black)
- Secondary Text: `#99000000` (60% black)
- List Items: `#CC000000` (80% black)

### Text Hierarchy
```xaml
<!-- Primary Heading -->
<TextBlock 
    FontSize="28"
    FontWeight="Bold"
    Foreground="#000000"/>

<!-- Secondary Heading -->
<TextBlock 
    FontSize="18"
    FontWeight="SemiBold"
    Foreground="#000000"/>

<!-- Normal Text -->
<TextBlock 
    FontSize="16"
    Foreground="#000000"/>

<!-- Caption Text -->
<TextBlock 
    FontSize="14"
    Foreground="#99000000"/>

<!-- List Items -->
<TextBlock 
    FontSize="14"
    Foreground="#CC000000"/>
```

## Custom Title Bar
```xaml
<Grid Height="32" 
      Background="Transparent"
      PointerPressed="OnTitleBarPointerPressed">
    <DockPanel LastChildFill="True">
        <Button Classes="modern-close"
                DockPanel.Dock="Right"/>
        <TextBlock Text="Window Title"
                  FontSize="13"
                  Foreground="#000000"
                  Margin="16,0"/>
    </DockPanel>
</Grid>
```
- Height: 32 pixels
- Title: 13px font size
- Close button: Right-aligned
- Draggable: Via `OnTitleBarPointerPressed` event

## Content Layout
```xaml
<Grid Margin="40,20,40,20">
    <StackPanel Spacing="16">
        <!-- Content here -->
    </StackPanel>
</Grid>
```
- Outer margin: 40px horizontal, 20px vertical
- Default spacing between elements: 16px

## Button Styles
- Include ModernButton styles:
```xaml
<Window.Styles>
    <StyleInclude Source="/Controls/ModernButton.axaml"/>
</Window.Styles>
```

Available button classes:
- `modern` - Standard white button
- `modern accent` - Blue accent button
- `modern-close` - Red close button
- `modern info` - Info blue button
- `modern success` - Success green button
- `modern warning` - Warning orange button
- `modern danger` - Danger red button
- `modern secondary` - Gray secondary button

## Usage Notes
1. Always include the ModernButton styles
2. Maintain consistent margins and spacing
3. Follow the text hierarchy for proper visual weight
4. Use the 3-layer background system for depth
5. Implement custom title bar for consistent look

This styling guide ensures consistency across all application windows while maintaining a modern, clean aesthetic with good readability and visual hierarchy. 