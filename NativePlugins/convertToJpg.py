import os
from PIL import Image
import numpy as np

SRC_DIR = os.path.join("PaperPlugin", "PaperPluginTester")
DST_DIR = os.path.join("AnnotatedTestImages")
os.makedirs(DST_DIR, exist_ok=True)

def read_rgba_image(filepath):
    with open(filepath, "rb") as f:
        data = f.read()
    arr = np.frombuffer(data, dtype=np.uint8)
    total_pixels = arr.size // 4
    # Try to guess square image dimensions
    side = int(total_pixels ** 0.5)
    if side * side * 4 != arr.size:
        print(f"Skipping {filepath}: cannot infer square dimensions from size {arr.size}")
        return None
    arr = arr.reshape((side, side, 4))
    return arr

for root, _, files in os.walk(SRC_DIR):
    for file in files:
        if file.startswith("_testimage") and file.endswith(".rgba"):
            rgba_path = os.path.join(root, file)
            rgba_data = read_rgba_image(rgba_path)
            if rgba_data is None:
                continue
            img = Image.fromarray(rgba_data, "RGBA")
            img = img.convert("RGB")
            jpg_name = os.path.splitext(file)[0] + ".jpg"
            jpg_path = os.path.join(DST_DIR, jpg_name)
            img.save(jpg_path, "JPEG")
            print(f"Converted {rgba_path} -> {jpg_path}")