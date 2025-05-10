#include <iostream>
#include <fstream>

// Declare the external function from the DLL
extern "C" bool FindPaperCorners(unsigned char* imageData, int width, int height, float* outCorners);

int main() {
    const int width = 640;
    const int height = 480;
    const int channels = 4; // RGBA
    const char* imagePath = "test_image.rgba"; // must be raw RGBA data

    // Load raw RGBA image from file
    std::ifstream file(imagePath, std::ios::binary);
    if (!file) {
        std::cerr << "Failed to open image file: " << imagePath << std::endl;
        return 1;
    }

    size_t dataSize = width * height * channels;
    unsigned char* buffer = new unsigned char[dataSize];
    file.read(reinterpret_cast<char*>(buffer), dataSize);
    file.close();

    if (!file) {
        std::cerr << "Failed to read full image data." << std::endl;
        delete[] buffer;
        return 1;
    }

    float corners[8];
    bool found = FindPaperCorners(buffer, width, height, corners);

    if (found) {
        std::cout << "Paper detected! Corners:" << std::endl;
        for (int i = 0; i < 4; ++i) {
            std::cout << "  Corner " << i << ": ("
                << corners[i * 2] << ", "
                << corners[i * 2 + 1] << ")" << std::endl;
        }
    }
    else {
        std::cout << "No paper detected." << std::endl;
    }

    delete[] buffer;
    return 0;
}

