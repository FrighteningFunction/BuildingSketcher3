#include <iostream>
#include <fstream>
#include <cmath>
#include <string>

#include <opencv2/opencv.hpp>

// Declare the external function from the DLL
extern "C" bool FindPaperCorners(unsigned char* imageData, int width, int height, float* outCorners);

cv::Mat EnsureBGR(const cv::Mat& img)
{
    if (img.channels() == 3)
        return img; // Already BGR

    cv::Mat bgr;
    if (img.channels() == 4)
        cv::cvtColor(img, bgr, cv::COLOR_BGRA2BGR);
    else if (img.channels() == 1)
        cv::cvtColor(img, bgr, cv::COLOR_GRAY2BGR);
    else
        bgr = img.clone();
    return bgr;
}


// Reusable helper: Draws filled circles ("dots") at the given points on an image.
void PlaceDotsOnImage(cv::Mat& img, const std::vector<cv::Point2f>& points, const cv::Scalar& color = cv::Scalar(0, 0, 255), int radius = 5)
{
    for (const auto& pt : points)
    {
        cv::circle(img, pt, radius, color, cv::FILLED);
    }
}

// Wrapper: Calls your plugin and converts the result to vector of cv::Point2f.
std::vector<cv::Point2f> DetectPaperCorners(cv::Mat& img)
{
    float outCorners[8] = {};
    bool found = FindPaperCorners(img.data, img.cols, img.rows, outCorners);
    std::vector<cv::Point2f> corners;
    if (found)
    {
        for (int i = 0; i < 4; ++i)
            corners.emplace_back(outCorners[i * 2], outCorners[i * 2 + 1]);
    }
    return corners;
}


void TestAndVisualizePaperDetection(const std::string& inputPath, const std::string& outputPath)
{
    // 1. Load image as RGBA
    cv::Mat img = cv::imread(inputPath, cv::IMREAD_UNCHANGED);
    if (img.empty())
    {
        std::cerr << "Could not load image: " << inputPath << std::endl;
        return;
    }
    // Ensure 4 channels (RGBA) for processing
    if (img.channels() != 4)
        cv::cvtColor(img, img, cv::COLOR_BGR2BGRA);

    // 2. Detect corners
    std::vector<cv::Point2f> corners = DetectPaperCorners(img);

    // 3. Visualize: place dots on corners (in-place)
    PlaceDotsOnImage(img, corners, cv::Scalar(0, 0, 255), 7); // Red dots, radius 7

    // 4. Convert to BGR for viewable JPEG, then save
    cv::Mat bgr = EnsureBGR(img);
    cv::imwrite(outputPath, bgr);

    std::cout << "Done. Saved: " << outputPath << std::endl;

	// 5. Display the result (optional)
    cv::imshow("Result", bgr);
    cv::waitKey(0);
}

int main()
{
    TestAndVisualizePaperDetection("TestFiles/papertest2.jpg", "output_with_corners.png");
    return 0;
}


