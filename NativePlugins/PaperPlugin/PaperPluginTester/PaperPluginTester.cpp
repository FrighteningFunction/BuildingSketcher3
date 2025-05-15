#include <iostream>
#include <fstream>
#include <cmath>
#include <string>

// Declare the external function from the DLL
extern "C" bool FindPaperCorners(unsigned char* imageData, int width, int height, float* outCorners);

// Draw a filled circle of given radius and color into the raw RGBA buffer
void DrawCircleRGBA(unsigned char* buf, int w, int h, int channels,
    int centerX, int centerY, int radius,
    unsigned char r, unsigned char g, unsigned char b, unsigned char a)
{
    for (int dy = -radius; dy <= radius; ++dy) {
        int y = centerY + dy;
        if (y < 0 || y >= h) continue;
        for (int dx = -radius; dx <= radius; ++dx) {
            int x = centerX + dx;
            if (x < 0 || x >= w) continue;
            if (dx * dx + dy * dy <= radius * radius) {
                size_t idx = (size_t)(y * w + x) * channels;
                buf[idx + 0] = r;
                buf[idx + 1] = g;
                buf[idx + 2] = b;
                buf[idx + 3] = a;
            }
        }
    }
}

int main() {
    const int width = 640;
    const int height = 480;
    const int channels = 4; // RGBA
    const char* imagePath = "test_image_drawing.rgba"; // raw RGBA data

    // Load raw RGBA image from file
    std::ifstream file(imagePath, std::ios::binary);
    if (!file) {
        std::cerr << "Failed to open image file: " << imagePath << std::endl;
        return 1;
    }
    size_t dataSize = static_cast<size_t>(width) * height * channels;
    unsigned char* buffer = new unsigned char[dataSize];
    file.read(reinterpret_cast<char*>(buffer), dataSize);
    file.close();
    if (file.gcount() != static_cast<std::streamsize>(dataSize)) {
        std::cerr << "Failed to read full image data." << std::endl;
        delete[] buffer;
        return 1;
    }

    // Detect paper corners
    float corners[8];
    bool found = FindPaperCorners(buffer, width, height, corners);

    if (found) {
        std::cout << "Paper detected! Corners:" << std::endl;

        // Draw a small red circle at each corner
        const int radius = 5;
        for (int i = 0; i < 4; ++i) {
            float fx = corners[i * 2 + 0];
            float fy = corners[i * 2 + 1];
            int ix = static_cast<int>(std::round(fx));
            int iy = static_cast<int>(std::round(fy));

            std::cout << "  Corner " << i << ": (" << fx << ", " << fy
                << ")  ⇒  pixel (" << ix << ", " << iy << ")\n";

            DrawCircleRGBA(buffer, width, height, channels,
                ix, iy, radius,
                255, 0, 0, 255);  // red, fully opaque
        }
    }
    else {
        std::cout << "No paper detected." << std::endl;
    }

    // Write out the modified image
    std::string outPath = std::string("_testimage_") + imagePath;
    std::ofstream outFile(outPath, std::ios::binary);
    if (!outFile) {
        std::cerr << "Failed to open output file: " << outPath << std::endl;
        delete[] buffer;
        return 1;
    }
    outFile.write(reinterpret_cast<char*>(buffer), dataSize);
    outFile.close();
    std::cout << "Wrote annotated image to: " << outPath << std::endl;

    // Write image dimensions to a .txt file with the same base name
    std::string txtPath = outPath;
    auto pos = txtPath.rfind('.');
    if (pos != std::string::npos) {
        txtPath = txtPath.substr(0, pos) + ".txt";
    }
    else {
        txtPath += ".txt";
    }

    std::ofstream txtFile(txtPath);
    if (!txtFile) {
        std::cerr << "Failed to open dimension file: " << txtPath << std::endl;
    }
    else {
        txtFile << "height: " << height << ", width: " << width;
        txtFile.close();
        std::cout << "Wrote image dimensions to: " << txtPath << std::endl;
    }

    delete[] buffer;
    return 0;
}