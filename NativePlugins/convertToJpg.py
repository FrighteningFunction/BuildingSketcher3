import os
from PIL import Image
import numpy as np

SRC_DIR = os.path.join("PaperPlugin", "PaperPluginTester")
DST_DIR = os.path.join("AnnotatedTestImages")
os.makedirs(DST_DIR, exist_ok=True)

def get_dimensions_from_txt(txt_path):
    try:
        with open(txt_path, "r") as f:
            line = f.read().strip()
            parts = line.replace(" ", "").split(",")
            height = int(parts[0].split(":")[1])
            width = int(parts[1].split(":")[1])
            return height, width
    except Exception as e:
        print(f"Could not read dimensions from {txt_path}: {e}")
        return None, None

def read_rgba_image(filepath, height, width):
    with open(filepath, "rb") as f:
        data = f.read()
    arr = np.frombuffer(data, dtype=np.uint8)
    expected_size = height * width * 4
    if arr.size != expected_size:
        print(f"Skipping {filepath}: size {arr.size} does not match expected {expected_size}")
        return None
    arr = arr.reshape((height, width, 4))
    return arr

for root, _, files in os.walk(SRC_DIR):
    for file in files:
        if file.startswith("_testimage") and file.endswith(".rgba"):
            rgba_path = os.path.join(root, file)
            txt_path = os.path.splitext(rgba_path)[0] + ".txt"
            height, width = get_dimensions_from_txt(txt_path)
            if not height or not width:
                continue
            rgba_data = read_rgba_image(rgba_path, height, width)
            if rgba_data is None:
                continue
            img = Image.fromarray(rgba_data, "RGBA")
            img = img.convert("RGB")
            jpg_name = os.path.splitext(file)[0] + ".jpg"
            jpg_path = os.path.join(DST_DIR, jpg_name)
            img.save(jpg_path, "JPEG")
            print(f"Converted {rgba_path} -> {jpg_path}")