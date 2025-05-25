#include "maze_map.h"
#include <iostream>
#include <vector>
#include <array>
#include <random>
#include <algorithm>
#include <string>
#include <set>
#include <functional>
#include <sstream>
#include <climits>
#include <fstream>

#define WALL '#'
#define FLOOR '.'
#define EXIT 'E'
#define CENTER 'C'
#define SPAWN 'S'
#define TREASURE '$'
#define REAPER 'R'

MazeMap::MazeMap(int width, int height)
    : width_(width), height_(height), maze_(height, std::vector<char>(width, WALL)) {}

void MazeMap::generate()
{
    auto [map, altar] = generate_maze_char(width_, height_);
    maze_ = map;
    altar_position_ = altar;
    add_loops_in_center_area(15, 10, 5);
    treasure_coords_ = place_treasures(15);
    spawn_points_ = mark_spawn_points();
}

std::string MazeMap::get_rle_compressed_maze() const
{
    std::stringstream ss;
    for (const auto &row : maze_)
    {
        char prev = row[0];
        int count = 1;
        for (size_t i = 1; i < row.size(); ++i)
        {
            if (row[i] == prev)
            {
                count++;
            }
            else
            {
                ss << count << prev;
                prev = row[i];
                count = 1;
            }
        }
        ss << count << prev << "\n";
    }
    return ss.str();
}

std::mt19937 &MazeMap::get_rng()
{
    static std::random_device rd;
    static std::mt19937 rng(rd());
    return rng;
}

std::pair<std::vector<std::vector<char>>, std::pair<int, int>> MazeMap::generate_maze_char(int width, int height)
{
    std::vector<std::vector<char>> maze(height, std::vector<char>(width, WALL));
    auto &rng = get_rng();

    int room_margin_x = width / 4;
    int room_margin_y = height / 4;

    std::uniform_int_distribution<int> cx_dist(room_margin_x, width - room_margin_x - 1);
    std::uniform_int_distribution<int> cy_dist(room_margin_y, height - room_margin_y - 1);

    int center_x = cx_dist(rng);
    int center_y = cy_dist(rng);

    center_x = std::max(2, std::min(width - 3, center_x));
    center_y = std::max(2, std::min(height - 3, center_y));

    int half_room = 1;

    for (int y = center_y - half_room; y <= center_y + half_room; ++y)
    {
        for (int x = center_x - half_room; x <= center_x + half_room; ++x)
        {
            maze[y][x] = FLOOR;
        }
    }

    maze[center_y][center_x] = CENTER;

    std::function<void(int, int)> carve = [&](int x, int y)
    {
        std::array<std::pair<int, int>, 4> dirs = {{{0, 2}, {0, -2}, {2, 0}, {-2, 0}}};
        std::shuffle(dirs.begin(), dirs.end(), rng);

        for (auto [dx, dy] : dirs)
        {
            int nx = x + dx, ny = y + dy;
            if (1 <= nx && nx < width - 1 && 1 <= ny && ny < height - 1 && maze[ny][nx] == WALL)
            {
                maze[ny][nx] = FLOOR;
                maze[y + dy / 2][x + dx / 2] = FLOOR;
                carve(nx, ny);
            }
        }
    };

    for (int i = 0; i < 3; ++i)
    {
        std::uniform_int_distribution<int> sx_dist(1, (width - 1) / 2 - 1);
        std::uniform_int_distribution<int> sy_dist(1, (height - 1) / 2 - 1);
        int sx = sx_dist(rng) * 2 + 1;
        int sy = sy_dist(rng) * 2 + 1;
        maze[sy][sx] = FLOOR;
        carve(sx, sy);
    }

    std::vector<std::pair<int, int>> exits;
    std::vector<std::pair<int, int>> possible_edges;

    for (int i = 1; i < width; i += 2)
    {
        possible_edges.emplace_back(0, i);
        possible_edges.emplace_back(height - 1, i);
    }

    for (int i = 1; i < height; i += 2)
    {
        possible_edges.emplace_back(i, 0);
        possible_edges.emplace_back(i, width - 1);
    }

    std::shuffle(possible_edges.begin(), possible_edges.end(), rng);

    for (auto [y, x] : possible_edges)
    {
        if (maze[y][x] == WALL && exits.size() < 4)
        {
            maze[y][x] = EXIT;
            exits.emplace_back(x, y);
        }
    }

    for (int y = center_y - half_room - 1; y <= center_y + half_room + 1; ++y)
    {
        for (int x = center_x - half_room - 1; x <= center_x + half_room + 1; ++x)
        {
            if (0 <= x && x < width && 0 <= y && y < height)
            {
                if (maze[y][x] == WALL)
                {
                    maze[y][x] = FLOOR;
                }
            }
        }
    }

    return {maze, {center_x, center_y}};
}

void MazeMap::add_loops_in_center_area(int radius, int count, int min_gap)
{
    int center_x = width_ / 2;
    int center_y = height_ / 2;

    std::vector<std::pair<int, int>> candidates;

    for (int y = center_y - radius; y <= center_y + radius; ++y)
    {
        for (int x = center_x - radius; x <= center_x + radius; ++x)
        {
            if (2 <= x && x < width_ - 2 && 2 <= y && y < height_ - 2)
            {
                if (abs(x - center_x) + abs(y - center_y) <= radius)
                {
                    if (maze_[y][x] == WALL)
                    {
                        if ((
                                (maze_[y][x - 1] == FLOOR || maze_[y][x - 1] == CENTER) &&
                                (maze_[y][x + 1] == FLOOR || maze_[y][x + 1] == CENTER) &&
                                maze_[y - 1][x] == WALL && maze_[y + 1][x] == WALL &&
                                maze_[y - 2][x] == WALL && maze_[y + 2][x] == WALL) ||
                            ((maze_[y - 1][x] == FLOOR || maze_[y - 1][x] == CENTER) &&
                             (maze_[y + 1][x] == FLOOR || maze_[y + 1][x] == CENTER) &&
                             maze_[y][x - 1] == WALL && maze_[y][x + 1] == WALL &&
                             maze_[y][x - 2] == WALL && maze_[y][x + 2] == WALL))
                        {
                            candidates.emplace_back(x, y);
                        }
                    }
                }
            }
        }
    }

    auto &rng = get_rng();
    std::shuffle(candidates.begin(), candidates.end(), rng);

    std::vector<std::pair<int, int>> chosen;
    while (!candidates.empty() && chosen.size() < size_t(count))
    {
        auto [x, y] = candidates.back();
        candidates.pop_back();
        maze_[y][x] = FLOOR;
        chosen.push_back({x, y});

        candidates.erase(
            std::remove_if(candidates.begin(), candidates.end(),
                           [x, y, min_gap](const auto &pt)
                           {
                               return abs(pt.first - x) + abs(pt.second - y) < min_gap;
                           }),
            candidates.end());
    }
}

std::vector<std::pair<int, int>> MazeMap::place_treasures(int count)
{
    int margin = 2;
    std::vector<std::pair<int, int>> candidates;

    for (int y = margin; y < height_ - margin; ++y)
    {
        for (int x = margin; x < width_ - margin; ++x)
        {
            if (maze_[y][x] == FLOOR || maze_[y][x] == CENTER || maze_[y][x] == EXIT)
            {
                candidates.emplace_back(x, y);
            }
        }
    }

    if (candidates.empty() || count == 0)
    {
        return {};
    }

    auto &rng = get_rng();
    std::vector<std::pair<int, int>> selected;

    std::uniform_int_distribution<int> dist(0, candidates.size() - 1);
    auto first = candidates[dist(rng)];
    selected.push_back(first);
    maze_[first.second][first.first] = TREASURE;

    auto min_dist = [&selected](const std::pair<int, int> &pt)
    {
        int d = INT_MAX;
        for (const auto &[sx, sy] : selected)
        {
            int curr_dist = (pt.first - sx) * (pt.first - sx) + (pt.second - sy) * (pt.second - sy);
            d = std::min(d, curr_dist);
        }
        return d;
    };

    while (selected.size() < size_t(count) && !candidates.empty())
    {
        auto farthest = std::max_element(candidates.begin(), candidates.end(),
                                         [&min_dist](const auto &a, const auto &b)
                                         {
                                             return min_dist(a) < min_dist(b);
                                         });

        if (farthest == candidates.end())
        {
            break;
        }

        selected.push_back(*farthest);
        maze_[farthest->second][farthest->first] = TREASURE;
        candidates.erase(farthest);
    }

    return selected;
}

std::vector<std::pair<int, int>> MazeMap::mark_spawn_points()
{
    std::array<std::pair<int, int>, 4> corners = {{{7, 7}, {7, width_ - 7}, {height_ - 7, 7}, {height_ - 7, width_ - 7}}};
    int ax = altar_position_.first, ay = altar_position_.second;

    std::vector<std::pair<std::pair<int, int>, int>> corners_with_dist;
    for (const auto &[cx, cy] : corners)
    {
        corners_with_dist.push_back({{cx, cy}, abs(cx - ax) + abs(cy - ay)});
    }

    std::sort(corners_with_dist.begin(), corners_with_dist.end(),
              [](const auto &a, const auto &b)
              { return a.second > b.second; });

    std::vector<std::pair<int, int>> farthest_corners;
    for (int i = 0; i < 3 && size_t(i) < corners_with_dist.size(); ++i)
    {
        farthest_corners.push_back(corners_with_dist[i].first);
    }

    std::vector<std::pair<int, int>> spawns;
    for (const auto &[cx, cy] : farthest_corners)
    {
        bool found = false;
        for (int dy = -3; dy <= 3 && !found; ++dy)
        {
            for (int dx = -3; dx <= 3 && !found; ++dx)
            {
                int nx = cx + dx, ny = cy + dy;
                if (0 <= nx && nx < width_ && 0 <= ny && ny < height_)
                {
                    if (maze_[ny][nx] == FLOOR || maze_[ny][nx] == CENTER || maze_[ny][nx] == EXIT)
                    {
                        maze_[ny][nx] = SPAWN;
                        spawns.emplace_back(nx, ny);
                        found = true;
                    }
                }
            }
        }
    }

    if (0 <= ax + 1 && ax + 1 < width_ && 0 <= ay && ay < height_)
    {
        if (maze_[ay][ax + 1] == FLOOR || maze_[ay][ax + 1] == CENTER || maze_[ay][ax + 1] == EXIT)
        {
            maze_[ay][ax + 1] = REAPER;
            spawns.emplace_back(ax + 1, ay);
        }
    }

    return spawns;
}

void MazeMap::print_maze() const
{
    for (const auto &row : maze_)
    {
        for (char cell : row)
        {
            std::cout << cell;
        }
        std::cout << std::endl;
    }
}

int MazeMap::get_width() const { return width_; }
int MazeMap::get_height() const { return height_; }
std::pair<int, int> MazeMap::get_altar_position() const { return altar_position_; }
const std::vector<std::pair<int, int>> &MazeMap::get_treasure_coords() const { return treasure_coords_; }
const std::vector<std::pair<int, int>> &MazeMap::get_spawn_points() const { return spawn_points_; }