---
name: Echoes of the Mind Design System
colors:
  surface: '#0c1324'
  surface-dim: '#0c1324'
  surface-bright: '#33394c'
  surface-container-lowest: '#070d1f'
  surface-container-low: '#151b2d'
  surface-container: '#191f31'
  surface-container-high: '#23293c'
  surface-container-highest: '#2e3447'
  on-surface: '#dce1fb'
  on-surface-variant: '#c7c4d6'
  inverse-surface: '#dce1fb'
  inverse-on-surface: '#2a3043'
  outline: '#918fa0'
  outline-variant: '#464554'
  surface-tint: '#c2c1ff'
  primary: '#c2c1ff'
  on-primary: '#1a09a1'
  primary-container: '#5d5cde'
  on-primary-container: '#f1eeff'
  inverse-primary: '#4e4cce'
  secondary: '#c4c1fb'
  on-secondary: '#2d2a5b'
  secondary-container: '#444173'
  on-secondary-container: '#b3afe9'
  tertiary: '#bec6e0'
  on-tertiary: '#283044'
  tertiary-container: '#656d84'
  on-tertiary-container: '#edf0ff'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#e2dfff'
  primary-fixed-dim: '#c2c1ff'
  on-primary-fixed: '#0b006b'
  on-primary-fixed-variant: '#3530b6'
  secondary-fixed: '#e3dfff'
  secondary-fixed-dim: '#c4c1fb'
  on-secondary-fixed: '#181445'
  on-secondary-fixed-variant: '#444173'
  tertiary-fixed: '#dae2fd'
  tertiary-fixed-dim: '#bec6e0'
  on-tertiary-fixed: '#131b2e'
  on-tertiary-fixed-variant: '#3f465c'
  background: '#0c1324'
  on-background: '#dce1fb'
  surface-variant: '#2e3447'
typography:
  headline-xl:
    fontFamily: Space Grotesk
    fontSize: 48px
    fontWeight: '300'
    lineHeight: '1.2'
    letterSpacing: 0.15em
  headline-md:
    fontFamily: Space Grotesk
    fontSize: 24px
    fontWeight: '400'
    lineHeight: '1.4'
    letterSpacing: 0.1em
  body-lg:
    fontFamily: Manrope
    fontSize: 18px
    fontWeight: '400'
    lineHeight: '1.6'
    letterSpacing: 0.05em
  body-sm:
    fontFamily: Manrope
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.6'
    letterSpacing: 0.02em
  label-caps:
    fontFamily: Space Grotesk
    fontSize: 12px
    fontWeight: '600'
    lineHeight: '1'
    letterSpacing: 0.25em
spacing:
  unit: 4px
  xs: 4px
  sm: 12px
  md: 24px
  lg: 48px
  xl: 96px
  safe-area: 64px
---

## Brand & Style

The design system is centered on the concept of cognitive isolation and the beauty of the unknown. It evokes a cerebral and melancholic atmosphere, designed to make the player feel small yet focused within a vast, abstract void. The aesthetic direction leverages a hybrid of **Minimalism** and **Glassmorphism**. 

The UI does not compete with the environment; instead, it acts as a fragile, translucent overlay—much like a flickering memory. High-contrast geometric structures represent the "logic" of the puzzles, while organic, ethereal glows and fog represent the "emotion" and "mystery" of the mind. Every interaction should feel intentional, quiet, and slightly haunting.

## Colors

This design system utilizes a "Void Palette." The foundation is built on absolute blacks and near-black indigos to create infinite depth. 

- **Foundation:** Deep blacks (#020617) provide the canvas of the "Mind."
- **Ethereal Layers:** Purples and blues are used primarily in gradients and blurs to simulate fog and "echo" silhouettes.
- **Interactive Accents:** Pure white or high-brightness cyan is reserved for critical path elements, interactive nodes, and puzzle feedback, ensuring immediate visual hierarchy against the dark background.
- **State Colors:** Use low-opacity versions of the secondary blue for inactive states to maintain a ghostly, receding appearance.

## Typography

Typography in this design system is used as a spatial element. By utilizing **Space Grotesk** for headlines and labels, the UI adopts a technical, cerebral tone. The generous letter spacing is mandatory; it creates a sense of "emptiness" between characters that mirrors the game's atmospheric themes.

**Manrope** is used for longer descriptions or instructional text to ensure legibility while maintaining a modern, refined feel. Text should rarely be 100% opaque unless it is a primary heading; use 70-80% opacity to help type bleed into the atmospheric fog of the background.

## Layout & Spacing

The layout philosophy follows a **Fixed Grid** approach with significant safe-area margins to emphasize the feeling of being "centered" in a void. 

- **Negative Space:** Elements should be pushed toward the periphery of the screen or centered with extreme padding to evoke isolation.
- **Rhythm:** A 4px base unit is used, but spacing should typically jump in large increments (md to lg) to avoid visual clutter.
- **Alignment:** UI elements should align to a strict 12-column grid for menus, while the HUD remains detached, floating at the corners with at least 64px of padding from the display edge.

## Elevation & Depth

Depth is not communicated through shadows, but through **Glassmorphism** and light filtration. 

- **Backdrop Blurs:** Use high-radius background blurs (20px-40px) on any container to simulate the UI existing within the "fog."
- **Tonal Layers:** Higher-level elements (like active modals) are not lighter in color; they are simply more translucent with a sharper, thin white border (0.5px).
- **Glows:** Instead of drop shadows, use "Outer Glows" using the primary blue color. This suggests the UI is an emission of light rather than a physical object.
- **Parallax:** In-game menus should have a slight parallax effect relative to the background fog to create a sense of floating layers.

## Shapes

The design system employs a **Sharp (0)** roundedness strategy. The UI is constructed of precise, geometric lines to contrast against the soft, organic nature of the environmental fog.

- **Geometric Contrast:** All buttons, containers, and HUD elements must have 90-degree corners. This reinforces the "Logic/Cerebral" aspect of the brand personality.
- **Lines:** Use thin, 1px lines for borders and separators. Avoid solid fills where a stroke can suffice. 
- **Icons:** Use monolinear, geometric icons with open paths to maintain the minimalist, airy aesthetic.

## Components

### Buttons
Buttons are "Ghost" style by default. They consist of a 1px white border with 10% white fill. On hover, the fill opacity increases to 20%, and the border gains a soft, ethereal blue outer glow. There is no "click" animation—only a subtle fade in light intensity.

### The 'Echo' HUD
The HUD elements (energy, memory fragments) are represented by thin, vertical or horizontal lines that deplete by fading into 0% opacity rather than moving. They should appear translucent and flicker slightly, as if they are unstable data.

### Cards & Modals
Containers use a dark, semi-transparent background with a heavy backdrop blur. They do not have headers; titles are placed above the container with wide letter spacing.

### Interactive Nodes
Puzzle elements that can be interacted with should be the only elements using the high-contrast Cyan accent. They should "pulse" with a soft glow to draw the eye without breaking the melancholic stillness of the scene.

### Lists
Lists are unstyled by default, using only indentation and increased vertical spacing (md) to separate items. A thin vertical line to the left of the active item is the only selection indicator.