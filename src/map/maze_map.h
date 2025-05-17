#pragma once

#include <vector>
#include <string>
#include <random>
#include <functional>
#include <array>
#include <set>

#define WALL '#'
#define FLOOR '.'
#define EXIT 'E'
#define CENTER 'C'
#define SPAWN 'S'
#define TREASURE '$'
#define REAPER 'R'

class MazeMap {
public:
    MazeMap(int width = 31, int height = 31);
    void generate();
    std::string get_rle_compressed_maze() const;
    void print_maze() const;
    int get_width() const;
    int get_height() const;
    std::pair<int, int> get_altar_position() const;
    const std::vector<std::pair<int, int>>& get_treasure_coords() const;
    const std::vector<std::pair<int, int>>& get_spawn_points() const;

private:
    std::mt19937& get_rng();
    std::pair<std::vector<std::vector<char>>, std::pair<int, int>> generate_maze_char(int width, int height);
    void add_loops_in_center_area(int radius, int count, int min_gap);
    std::vector<std::pair<int, int>> place_treasures(int count);
    std::vector<std::pair<int, int>> mark_spawn_points();

    int width_;
    int height_;
    std::vector<std::vector<char>> maze_;
    std::pair<int, int> altar_position_;
    std::vector<std::pair<int, int>> treasure_coords_;
    std::vector<std::pair<int, int>> spawn_points_;
}; 