// LineDetector.cpp

#include "pch.h"
#include <opencv2/opencv.hpp>
#include <vector>

extern "C" {
    // imageData: pointer to RGBA image data (unsigned char*, width*height*4)
    // width, height: image dimensions
    // outLines: pointer to output float array, format: [x1, y1, x2, y2, x1, y1, x2, y2, ...]
    // maxLines: maximum number of lines to export (each line = 4 floats)
    // Returns: actual number of lines written to outLines
    int FindBlackLines(unsigned char* imageData, int width, int height, float* outLines, int maxLines);
}

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

    // Use HoughLinesP to find straight lines
    std::vector<cv::Vec4i> lines;
    cv::HoughLinesP(edges, lines, 1, CV_PI / 180, 50, 50, 10);

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
