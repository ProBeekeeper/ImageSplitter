# ImageSplitter

A clean, minimalist, and high-performance desktop utility built with .NET 10 Windows Forms, designed to partition images into precise matrix grids with zero quality loss.

---

## Key Features

* **Lossless Export**: Forces 100% quality retention for JPEG encoders to ensure pixel-perfect artifacts-free clipping.
* **Live Grid Alignment**: Real-time visual overlay showing exact slicing coordinates dynamically.
* **Batch Concurrency**: Leverages low-level hardware parallelism via a thread-safe processing pipeline to handle massive multi-file workloads efficiently.
* **Collision-Free Storage**: Features an advanced folder indexing algorithm to prevent accidental data overwrites during repetitive slicing tasks.
* **Zero-Dependency Architecture**: Compiled as a native single-executable with all multi-language localization streams embedded directly inside the binary assembly.

---

## How to Use

### 1. Interface Initialization
When launched, the application initializes in a compact 540x480 canvas. The default system language is English (`en-US`). If you need to switch languages, use the dropdown menu in the top-right corner; the interface strings will re-render instantly based on the embedded JSON streams.

### 2. Configuration & Live Preview
Locate the parameter fields on the top-left section to set your grid dimensions:
* **Rows**: Specifies the number of horizontal slices.
* **Columns (Cols)**: Specifies the number of vertical slices.

You can adjust these values using the step arrows or by typing values directly. The input engine is bound to a 300ms debounce timer; it delays rendering for 0.3 seconds after your last keystroke to ensure the UI remains smooth and responsive without layout lag.

### 3. File Import Workflow
Click **"Select Images"** to trigger the file dialog. The interface filters for common asset extensions (`.png`, `.jpg`, `.jpeg`, `.bmp`, `.webp`). You can import a single asset or select a collection of files simultaneously.

* **Single File Behavior**: If you import a single image (e.g., `Beekeeper.png`) with the preview box enabled, the canvas expands to a widescreen 960x480 layout. The newly revealed right-hand panel renders a dynamic red grid over the image, showing exactly where the cuts will occur.
* **Batch Files Behavior**: If multiple images are imported, the live preview and checkbox are automatically suspended to optimize memory management and system execution speed.

The console terminal will log the number of staged files, and the **"Start Splitting"** button will unlock.

### 4. Concurrent Execution
Click **"Start Splitting"** to initiate the slicing pipeline:
1. The engine checks the directory of the source image. For an asset named `Beekeeper.png`, it prepares a target folder named `Beekeeper`.
2. If you re-slice the same file using different grid configurations, the application increments the directory name to `Beekeeper (2)`, safeguarding your older data from overwrites.
3. The workload is distributed via `Parallel.ForEach`. Slices are named sequentially (e.g., `Beekeeper(1).png`, `Beekeeper(2).png`) and saved asynchronously.
4. The progress bar reflects real-time task status. Once complete, a message box signals that the archive is ready.

---

## Acknowledgments

* **Inspiration**: This project was inspired by a practical requirement from my friend, **KingLo**, aimed at optimizing everyday digital and simulation workflows.

---

## Security & SmartScreen Notice

Because this is an open-source tool distributed without a costly commercial Code Signing Certificate, Windows SmartScreen may trigger a "Windows protected your PC" warning upon the first launch. 

**This is normal behavior for unsigned open-source binaries.** You can safely bypass this by clicking **"More info"** and then selecting **"Run anyway"**. Since the entire source code is fully transparent and hosted publicly here, you can audit the repository at any time.
