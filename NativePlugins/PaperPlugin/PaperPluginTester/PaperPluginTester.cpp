#include <iostream>
#include <fstream>
#include <cmath>
#include <string>

#include <opencv2/opencv.hpp>

// Declare the external function from the DLL
extern "C" bool FindPaperCorners(unsigned char* imageData, int width, int height, float* outCorners);

extern "C" __declspec(dllimport)
int FindBlackLines(unsigned char* imageData, int width, int height, float* outLines, int maxLines);

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

// Wrapper: Calls plugin and converts the result to vector of cv::Point2f.
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
// Wrapper: Calls plugin to detect black lines
std::vector<std::pair<cv::Point2f, cv::Point2f>> DetectBlackLines(cv::Mat& img, int maxLines = 100)
{
    std::vector<std::pair<cv::Point2f, cv::Point2f>> lines;
    std::vector<float> outLines(maxLines * 4, 0.0f);

    int found = FindBlackLines(img.data, img.cols, img.rows, outLines.data(), maxLines);

    for (int i = 0; i < found; ++i)
    {
        cv::Point2f pt1(outLines[i * 4 + 0], outLines[i * 4 + 1]);
        cv::Point2f pt2(outLines[i * 4 + 2], outLines[i * 4 + 3]);
        lines.emplace_back(pt1, pt2);
    }
    return lines;
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

void TestAndVisualizeLineDetection(const std::string& inputPath, const std::string& outputPath)
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

    // 2. Detect lines
    auto lines = DetectBlackLines(img);

    // 3. Visualize: draw each line segment, place dots at endpoints
    cv::Scalar lineColor(0, 255, 0); // Green lines
    cv::Scalar dotColor(255, 0, 0);  // Blue dots (for endpoints)

    for (const auto& seg : lines)
    {
        cv::line(img, seg.first, seg.second, lineColor, 2);
    }
    // Optionally, also visualize the endpoints as dots
    std::vector<cv::Point2f> endpoints;
    for (const auto& seg : lines)
    {
        endpoints.push_back(seg.first);
        endpoints.push_back(seg.second);
    }
    PlaceDotsOnImage(img, endpoints, dotColor, 5);

    // 4. Convert to BGR for viewable JPEG, then save
    cv::Mat bgr = EnsureBGR(img);
    cv::imwrite(outputPath, bgr);

    std::cout << "Done. Saved: " << outputPath << std::endl;

    // 5. Display the result (optional)
    cv::imshow("Lines Result", bgr);
    cv::waitKey(0);
}

int main()
{
    //TestAndVisualizePaperDetection("TestFiles/papertest2.jpg", "output_with_corners.png");
	TestAndVisualizeLineDetection("TestFiles/linetest8.png", "output_with_lines.png");
    return 0;
}


