#pragma once
#include <cmath>

// -------- Vector2 --------
struct Vector2 {
    float x, y;

    Vector2(float x = 0.0f, float y = 0.0f) : x(x), y(y) {}

    float length() const { return std::sqrt(x * x + y * y); }

    Vector2 normalized() const {
        float len = length();
        return len == 0 ? Vector2(0, 0) : Vector2(x / len, y / len);
    }

    Vector2 operator+(const Vector2& other) const { return Vector2(x + other.x, y + other.y); }
    Vector2 operator-(const Vector2& other) const { return Vector2(x - other.x, y - other.y); }
    Vector2 operator*(float scalar) const { return Vector2(x * scalar, y * scalar); }
    Vector2 operator/(float scalar) const { return Vector2(x / scalar, y / scalar); }

    float dot(const Vector2& other) const { return x * other.x + y * other.y; }
};

// -------- Vector3 --------
struct Vector3 {
    float x, y, z;

    Vector3(float x = 0.f, float y = 0.f, float z = 0.f) : x(x), y(y), z(z) {}

    float length() const { return std::sqrt(x * x + y * y + z * z); }

    Vector3 normalized() const {
        float len = length();
        return len == 0 ? Vector3(0, 0, 0) : Vector3(x / len, y / len, z / len);
    }

    Vector3 operator+(const Vector3& other) const { return Vector3(x + other.x, y + other.y, z + other.z); }
    Vector3 operator-(const Vector3& other) const { return Vector3(x - other.x, y - other.y, z - other.z); }
    Vector3 operator*(float scalar) const { return Vector3(x * scalar, y * scalar, z * scalar); }
    Vector3 operator/(float scalar) const { return Vector3(x / scalar, y / scalar, z / scalar); }

    float dot(const Vector3& other) const { return x * other.x + y * other.y + z * other.z; }
};