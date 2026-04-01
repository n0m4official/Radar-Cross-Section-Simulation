# SpecterCS

### *RCS Simulation Platform — “Reveal the Invisible.”*

---

## Overview

**SpecterCS** is a real-time radar cross-section (RCS) simulation platform built in C#. It models electromagnetic scattering using **Physical Optics (PO)** and **edge diffraction (PTD-inspired)** techniques, with support for **parallel CPU computation** and **GPU acceleration**.

The system provides interactive 3D visualization, enabling analysis of radar signatures across varying frequencies, azimuth, and elevation angles.

---

## Screenshot

<p align="center">
  <img src="Screenshot 2026-04-01 115417.png" width="520" alt="Active software running using a model of an E-2D Hawkeye">
  <br>
  <em>Active software running using a model of an E-2D Hawkeye</em>
</p>

---

## Core Features

* **Real-Time RCS Computation**

  * Physical Optics (facet-based scattering)
  * Edge diffraction modeling

* **High Performance Architecture**

  * Multi-threaded CPU processing
  * GPU acceleration via ComputeSharp

* **3D Visualization**

  * Interactive viewport (HelixToolkit)
  * Heatmap rendering of RCS distribution (dBsm)

* **Geometry Pipeline**

  * OBJ and STL import support
  * Automatic triangulation
  * Edge extraction for diffraction modeling
  * Mesh decimation (LOD system)

* **Radar Simulation**

  * Adjustable frequency (GHz range)
  * Configurable azimuth & elevation
  * Real-time sweep mode

* **Caching System**

  * Angular and frequency quantization
  * Fast recomputation for repeated scans

---

## Architecture

SpecterCS is structured as a modular system:

```
SpecterCS.Core
 ├── Engine        # RCS computation (PO + diffraction)
 ├── Geometry      # Mesh, facets, edges, LOD
 ├── Radar         # Radar configuration & physics
 ├── Import        # OBJ / STL loaders
 └── Gpu           # GPU compute pipeline

SpecterCS.Wpf
 ├── Rendering     # Heatmap + 3D scene
 ├── Controls      # UI components
 └── ViewModels    # MVVM structure (in progress)
```

---

## Simulation Model

### Physical Optics (PO)

Facet-based scattering is computed using:

* Surface normals
* Incident wave direction
* Phase accumulation
* Coherent summation

### Edge Diffraction

A simplified PTD-based model is used to approximate diffraction effects along mesh edges:

* Wedge angle estimation
* Incident and scatter direction projection
* Phase-based contribution

---

## Getting Started

### Requirements

* .NET 6+ or later
* Windows (required for GPU acceleration)
* GPU with DirectX 12 support (optional but recommended)

---

### Run the Application

1. Download the archive
2. Unzip archive (DO NOT MOVE EXE FROM FOLDER)
3. Double click `Echo1.Wpf.exe`
4. Enjoy

---

### Load a Model

* Click **“Load OBJ / STL”**
* Select a 3D mesh file
* The system will:

  * Import geometry
  * Build edges
  * Generate LODs
  * Begin real-time simulation

---

## Controls

| Control                    | Function          |
| -------------------------- | ----------------- |
| Mouse (Right Click + Drag) | Rotate camera     |
| W / A / S / D              | Move camera       |
| Q / E                      | Vertical movement |
| Shift                      | Faster movement   |

---

## Output

* **RCS (dBsm)** — logarithmic radar signature
* **RCS (m²)** — linear radar cross-section
* **Heatmap Visualization** — per-facet contribution

---

## Current Limitations

* Simplified diffraction model (PTD approximation)
* No material properties (perfect conductor assumption)
* No polarization-specific modeling (planned)
* Limited validation against real-world datasets

---

## Roadmap

* [ ] Advanced diffraction models (full PTD / UTD)
* [ ] Material and dielectric modeling
* [ ] Polarization handling (HH, VV, HV, VH)
* [ ] Polar RCS plots
* [ ] Time-domain simulation
* [ ] Cloud / distributed computation
* [ ] AI-assisted stealth optimization

---

## Design Philosophy

SpecterCS is built around three principles:

* **Clarity** — visualize complex electromagnetic behavior intuitively
* **Performance** — leverage parallelism and GPU acceleration
* **Extensibility** — modular design for future expansion

---

## Status

Active development — evolving toward a professional-grade simulation platform.

---

## Disclaimer

This project is intended for **educational, research, and visualization purposes only**. It is not a validated engineering tool and should not be used for real-world defense or safety-critical applications.

---

## Author

Developed by **Mathew Dixon**

---
