---
name: Clarity & Knowledge
colors:
  surface: '#f8f9ff'
  surface-dim: '#d0dbed'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e6eeff'
  surface-container-high: '#dee9fc'
  surface-container-highest: '#d9e3f6'
  on-surface: '#121c2a'
  on-surface-variant: '#464555'
  inverse-surface: '#27313f'
  inverse-on-surface: '#eaf1ff'
  outline: '#777587'
  outline-variant: '#c7c4d8'
  surface-tint: '#4d44e3'
  primary: '#3525cd'
  on-primary: '#ffffff'
  primary-container: '#4f46e5'
  on-primary-container: '#dad7ff'
  inverse-primary: '#c3c0ff'
  secondary: '#00687a'
  on-secondary: '#ffffff'
  secondary-container: '#57dffe'
  on-secondary-container: '#006172'
  tertiary: '#571ac0'
  on-tertiary: '#ffffff'
  tertiary-container: '#6f3dd9'
  on-tertiary-container: '#e3d5ff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e2dfff'
  primary-fixed-dim: '#c3c0ff'
  on-primary-fixed: '#0f0069'
  on-primary-fixed-variant: '#3323cc'
  secondary-fixed: '#acedff'
  secondary-fixed-dim: '#4cd7f6'
  on-secondary-fixed: '#001f26'
  on-secondary-fixed-variant: '#004e5c'
  tertiary-fixed: '#e9ddff'
  tertiary-fixed-dim: '#d0bcff'
  on-tertiary-fixed: '#23005c'
  on-tertiary-fixed-variant: '#5516be'
  background: '#f8f9ff'
  on-background: '#121c2a'
  surface-variant: '#d9e3f6'
typography:
  display:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: '1.1'
    letterSpacing: -0.02em
  h1:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.015em
  h2:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  h3:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: '1.4'
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: '1.6'
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.5'
  label-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: '1.2'
    letterSpacing: 0.05em
  button:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '600'
    lineHeight: '1'
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 8px
  container-max: 1120px
  gutter: 24px
  margin: 32px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
  section-padding: 64px
---

## Brand & Style

The visual identity of this design system centers on **Minimalism** with a **Modern/Corporate** precision tailored for educational focus. The intent is to remove cognitive load, allowing the user to concentrate entirely on the content of the quizzes. The aesthetic is "Typography-first," where the hierarchy of information is communicated through weight and scale rather than decorative elements. 

The emotional tone is professional yet encouraging. It avoids the cluttered "gamification" style of typical trivia apps in favor of a sophisticated learning environment. The UI feels airy and intentional, utilizing generous whitespace to separate concepts and promote a sense of calm during timed assessments.

## Colors

The palette is anchored by a deep Indigo primary, representing stability and intelligence. The background strategy employs a very subtle gradient transition from pure white to a soft blue-tinted gray (#f9fafb), creating a "paper-like" depth that reduces eye strain.

- **Primary (Indigo):** Used for primary actions, progress indicators, and active states.
- **Accents (Cyan/Purple):** Cyan is reserved for success states, hints, or informative callouts. Purple is used for secondary achievements or specific category tagging.
- **Text (Dark Gray):** High-contrast dark gray (#1f2937) ensures AA/AAA accessibility for all body and headline content.
- **Surface:** White is used for interactive cards and input fields to make them "pop" against the light gray background.

## Typography

This design system utilizes **Inter** exclusively to leverage its exceptional legibility and neutral, utilitarian character. 

- **Headlines:** Use tighter letter-spacing and heavier weights to create a strong visual anchor for question titles and section headers.
- **Body Text:** Set with generous line height (1.5-1.6) to facilitate rapid reading of quiz options and long-form explanations.
- **Labels:** Small caps or increased letter spacing should be used for metadata like "Question 1 of 10" or category tags to distinguish them from actionable text.

## Layout & Spacing

A **Fixed Grid** model is used for the core quiz experience to maintain a centered, focused column that prevents line lengths from becoming too wide. 

- **Grid:** A 12-column layout with a 1120px max-width for desktop, collapsing to a single fluid column on mobile.
- **Rhythm:** An 8px linear scale governs all padding and margins. Vertical stacking of quiz answers should use the `stack-md` (16px) spacing to ensure clear tap targets.
- **Whitespace:** Emphasize top-of-page clearance (`section-padding`) to give the headers room to breathe, reinforcing the minimal aesthetic.

## Elevation & Depth

Hierarchy is established through **Ambient Shadows** and **Tonal Layers**. 

- **Surface Elevation:** The primary background is the lowest layer. Content containers (cards) sit on an elevated white surface.
- **Shadows:** Use extremely soft, diffused shadows with a low-opacity indigo tint (e.g., `rgba(79, 70, 229, 0.08)`). Shadows should have a large blur radius (15-30px) and minimal offset to feel natural and modern.
- **Dividers:** Use 1px borders in a very light gray (#e5e7eb) only when necessary. Preference should be given to whitespace over lines to separate content.

## Shapes

The shape language is friendly yet structured, utilizing **Rounded** corners to soften the professional tone.

- **Components:** Standard buttons and input fields utilize an 8px (0.5rem) radius.
- **Large Containers:** Content cards and quiz modules utilize a 16px (1rem) radius to create a distinct "pod" feel.
- **Icons:** Use simple outline icons with a consistent 2px stroke weight and slightly rounded caps to match the UI's geometry.

## Components

### Buttons
Primary buttons are solid Indigo with white text. Secondary buttons use a subtle gray background or a simple outline. All buttons must include a hover state that uses a slight "lift" effect (moving -2px on the Y-axis) and a scale-up of 1.02x to provide tactile feedback.

### Quiz Answer Cards
Answer choices are represented as large, white cards with a 1px border. On hover, the border changes to the primary Indigo. When selected, the card gains a soft Indigo glow and a subtle scale-down "pressed" animation.

### Progress Indicators
Progress bars are thin (4px-8px) and use a rounded track. The progress fill should use a horizontal gradient from Cyan to Indigo to visualize momentum.

### Input Fields
Forms and text inputs are clean with a focus state that highlights the border in Indigo and adds a soft blue outer glow (box-shadow). Labels sit clearly above the input in the `label-sm` style.

### Chips
Used for quiz categories or difficulty levels. These are pill-shaped with a low-opacity version of the accent colors (e.g., light cyan background with dark cyan text) to keep them secondary to the main content.