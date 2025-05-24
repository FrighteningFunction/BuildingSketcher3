// LineDetector.cpp

#include "pch.h"
#include <opencv2/opencv.hpp>
#include <vector>

extern "C" __declspec(dllexport)
int FindBlackLines(unsigned char* imageData, int width, int height, float* outLines, int maxLines)
{
    cv::Mat img(height, width, CV_8UC4, imageData);
    cv::Mat gray;
    cv::cvtColor(img, gray, cv::COLOR_BGR2GRAY);


    cv::Mat blurred;
    cv::GaussianBlur(gray, blurred, cv::Size(9, 9), 0);


    cv::Mat bw;
    cv::adaptiveThreshold(blurred, bw, 255, cv::ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY_INV, 21, 10);

    // Use Canny edge detector to emphasize edges
    cv::Mat edges;
    cv::Canny(bw, edges, 50, 200, 3);

    cv::Mat closed;
    cv::morphologyEx(edges, closed, cv::MORPH_CLOSE, cv::Mat(), cv::Point(-1, -1), 2);

    // Use HoughLinesP to find straight lines
    std::vector<cv::Vec4i> lines;

	int minLineLength = 250;
	int maxLineGap = 50;
    int houghThreshold = 90;

    cv::HoughLinesP(closed, lines, 1, CV_PI / 180, houghThreshold, minLineLength, maxLineGap);

    int linesExported = 0;
    for (size_t i = 0; i < lines.size() && linesExported < maxLines; ++i)
    {
        const cv::Vec4i& l = lines[i];
        outLines[linesExported * 4 + 0] = static_cast<float>(l[0]); // x1
        outLines[linesExported * 4 + 1] = static_cast<float>(l[1]); // y1
        outLines[linesExported * 4 + 2] = static_cast<float>(l[2]); // x2
        outLines[linesExported * 4 + 3] = static_cast<float>(l[3]); // y2
        ++linesExported;
    }

    return linesExported;
}
