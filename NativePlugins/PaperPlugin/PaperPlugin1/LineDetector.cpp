// LineDetector.cpp

#include "pch.h"
#include <opencv2/opencv.hpp>
#include <vector>

extern "C" __declspec(dllexport)
int FindBlackLines(unsigned char* imageData, int width, int height, float* outLines, int maxLines)
{
    cv::Mat img(height, width, CV_8UC4, imageData);
    cv::Mat gray;
    cv::cvtColor(img, gray, cv::COLOR_RGBA2GRAY);

    // Invert image if black lines are drawn on white paper
    // (optional - depends on your data, remove if unnecessary)
    cv::Mat inverted;
    cv::bitwise_not(gray, inverted);

    // Threshold to get black lines (if necessary)
    cv::Mat bw;
    cv::threshold(inverted, bw, 50, 255, cv::THRESH_BINARY);

    // Use Canny edge detector to emphasize edges
    cv::Mat edges;
    cv::Canny(bw, edges, 50, 200, 3);

    cv::Mat closed;
    cv::morphologyEx(edges, closed, cv::MORPH_CLOSE, cv::Mat(), cv::Point(-1, -1), 2);

    // Use HoughLinesP to find straight lines
    std::vector<cv::Vec4i> lines;

	int minLineLength = 350;
	int maxLineGap = 30;
    int houghThreshold = 100;

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
