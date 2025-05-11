#include <opencv2/opencv.hpp>


bool FindPaperCorners(unsigned char* imageData,
                      int width, int height,
                      float* outCorners)
{
    cv::Mat img(height, width, CV_8UC4, imageData);

    cv::Mat gray;
    cv::cvtColor(img, gray, cv::COLOR_RGBA2GRAY);
    cv::GaussianBlur(gray, gray, { 5,5 }, 0);

    // 1. Edge or adaptive threshold
    cv::Mat edges;
    cv::Canny(gray, edges, 50, 150);
    // Alternatively:
    // cv::threshold(gray, edges, 0, 255, cv::THRESH_BINARY | cv::THRESH_OTSU);

    // 2. Find contours on the binary mask
    std::vector<std::vector<cv::Point>> contours;
    cv::findContours(edges, contours,
                     cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

    // 3. Sort by area (largest first)
    std::sort(contours.begin(), contours.end(),
              [](auto& a, auto& b) {
                  return cv::contourArea(a) > cv::contourArea(b);
              });

    for (const auto& c : contours) {
        double peri = cv::arcLength(c, true);
        std::vector<cv::Point> approx;
        cv::approxPolyDP(c, approx, 0.02 * peri, true);

        if (approx.size() == 4 &&
            cv::isContourConvex(approx) &&
            cv::contourArea(approx) > 10000)       // optional area filter
        {
            // (Optional) order the corner list here

            for (int i = 0; i < 4; ++i) {
                outCorners[i * 2] = static_cast<float>(approx[i].x);
                outCorners[i * 2 + 1] = static_cast<float>(approx[i].y);
            }
            return true;
        }
    }
    return false;
}//
// Created by szoko on 5/11/2025.
//
