using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sea_batlle
{
    public class Cell : INotifyPropertyChanged
    {
        private string _text;
        private Brush _color = Brushes.LightBlue;

        public int X { get; set; }
        public int Y { get; set; }
        public bool HasShip { get; set; }
        public bool IsHit { get; set; }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public Brush Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class MainWindow : Window
    {
        private Cell[,] _playerCells = new Cell[10, 10];
        private Cell[,] _computerCells = new Cell[10, 10];
        private Random _random = new Random();
        private int _playerShipsRemaining = 20;
        private int _computerShipsRemaining = 20;

        private int _lastHitX = -1;
        private int _lastHitY = -1;
        private bool _huntingMode = false;
        private List<Tuple<int, int>> _possibleTargets = new List<Tuple<int, int>>();
        private Direction _currentDirection = Direction.None;
        private bool _directionFound = false;
        private bool _playerTurn = true;

        private enum Direction
        {
            None,
            Horizontal,
            Vertical
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
            RestartButton.Visibility = Visibility.Collapsed;
        }

        private void InitializeGame()
        {
            RestartButton.Visibility = Visibility.Collapsed;
            _playerShipsRemaining = 20;
            _computerShipsRemaining = 20;
            _huntingMode = false;
            _possibleTargets.Clear();
            _lastHitX = -1;
            _lastHitY = -1;
            _currentDirection = Direction.None;
            _directionFound = false;
            _playerTurn = true;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    _playerCells[i, j] = new Cell { X = i, Y = j };
                    _computerCells[i, j] = new Cell { X = i, Y = j };
                }
            }

            PlayerField.ItemsSource = _playerCells.Cast<Cell>();
            ComputerField.ItemsSource = _computerCells.Cast<Cell>();

            PlaceShipsWithSpacing(_playerCells);
            PlaceShipsWithSpacing(_computerCells);

            ShowPlayerShips();
        }

        private void PlaceShipsWithSpacing(Cell[,] cells)
        {
            PlaceShipWithSpacing(cells, 4);
            PlaceShipWithSpacing(cells, 3); PlaceShipWithSpacing(cells, 3);
            PlaceShipWithSpacing(cells, 2); PlaceShipWithSpacing(cells, 2); PlaceShipWithSpacing(cells, 2);
            for (int i = 0; i < 4; i++) PlaceShipWithSpacing(cells, 1);
        }

        private void PlaceShipWithSpacing(Cell[,] cells, int size)
        {
            bool placed = false;
            int attempts = 0;
            while (!placed && attempts < 100)
            {
                attempts++;
                int x = _random.Next(10);
                int y = _random.Next(10);
                bool horizontal = _random.Next(2) == 0;

                if (CanPlaceShipWithSpacing(cells, x, y, size, horizontal))
                {
                    for (int i = 0; i < size; i++)
                    {
                        if (horizontal) cells[x + i, y].HasShip = true;
                        else cells[x, y + i].HasShip = true;
                    }
                    placed = true;
                }
            }
        }

        private bool CanPlaceShipWithSpacing(Cell[,] cells, int x, int y, int size, bool horizontal)
        {
            if (horizontal && x + size > 10) return false;
            if (!horizontal && y + size > 10) return false;

            for (int i = Math.Max(0, x - 1); i <= Math.Min(9, x + (horizontal ? size : 1)); i++)
            {
                for (int j = Math.Max(0, y - 1); j <= Math.Min(9, y + (horizontal ? 1 : size)); j++)
                {
                    if (cells[i, j].HasShip) return false;
                }
            }
            return true;
        }

        private void ShowPlayerShips()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (_playerCells[i, j].HasShip)
                    {
                        _playerCells[i, j].Color = Brushes.LightGray;
                    }
                }
            }
        }

        private void ComputerCell_Click(object sender, RoutedEventArgs e)
        {
            if (!_playerTurn) return;

            var button = (Button)sender;
            var cell = (Cell)button.Tag;

            if (cell.IsHit) return;

            cell.IsHit = true;
            if (cell.HasShip)
            {
                cell.Color = Brushes.Red;
                cell.Text = "X";
                _computerShipsRemaining--;

                if (IsShipSunk(_computerCells, cell.X, cell.Y))
                {
                    MarkSunkShip(_computerCells, cell.X, cell.Y, Brushes.DarkRed);
                }

                CheckGameOver();
            }
            else
            {
                cell.Color = Brushes.White;
                cell.Text = "•";
                _playerTurn = false;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ComputerTurn();
                    _playerTurn = true;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void ComputerTurn()
        {
            int x, y;

            if (_huntingMode)
            {
                if (_possibleTargets.Count > 0)
                {
                    var target = _possibleTargets[0];
                    _possibleTargets.RemoveAt(0);
                    x = target.Item1;
                    y = target.Item2;
                }
                else
                {
                    FindNewTargets();
                    if (_possibleTargets.Count > 0)
                    {
                        var target = _possibleTargets[0];
                        _possibleTargets.RemoveAt(0);
                        x = target.Item1;
                        y = target.Item2;
                    }
                    else
                    {
                        _huntingMode = false;
                        _currentDirection = Direction.None;
                        _directionFound = false;
                        do
                        {
                            x = _random.Next(10);
                            y = _random.Next(10);
                        } while (_playerCells[x, y].IsHit);
                    }
                }
            }
            else
            {
                do
                {
                    x = _random.Next(10);
                    y = _random.Next(10);
                } while (_playerCells[x, y].IsHit);
            }

            _playerCells[x, y].IsHit = true;

            if (_playerCells[x, y].HasShip)
            {
                _playerCells[x, y].Color = Brushes.Red;
                _playerCells[x, y].Text = "X";
                _playerShipsRemaining--;

                _lastHitX = x;
                _lastHitY = y;
                _huntingMode = true;

                AddPossibleTargets(x, y);

                if (IsShipSunk(_playerCells, x, y))
                {
                    MarkSunkShip(_playerCells, x, y, Brushes.DarkRed);
                    _huntingMode = false;
                    _possibleTargets.Clear();
                    _currentDirection = Direction.None;
                    _directionFound = false;
                }
                else if (!_directionFound)
                {
                    DetermineDirection(x, y);
                }

                CheckGameOver();

                Dispatcher.BeginInvoke(new Action(ComputerTurn), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                _playerCells[x, y].Color = Brushes.White;
                _playerCells[x, y].Text = "•";

                if (_huntingMode && _currentDirection != Direction.None)
                {
                    ReverseDirection();
                }
            }
        }

        private void AddPossibleTargets(int x, int y)
        {
            if (x > 0 && !_playerCells[x - 1, y].IsHit)
                _possibleTargets.Add(Tuple.Create(x - 1, y));
            if (x < 9 && !_playerCells[x + 1, y].IsHit)
                _possibleTargets.Add(Tuple.Create(x + 1, y));
            if (y > 0 && !_playerCells[x, y - 1].IsHit)
                _possibleTargets.Add(Tuple.Create(x, y - 1));
            if (y < 9 && !_playerCells[x, y + 1].IsHit)
                _possibleTargets.Add(Tuple.Create(x, y + 1));
        }

        private void FindNewTargets()
        {
            _possibleTargets.Clear();

            if (_currentDirection == Direction.Horizontal)
            {
                if (_lastHitX > 0 && !_playerCells[_lastHitX - 1, _lastHitY].IsHit)
                    _possibleTargets.Add(Tuple.Create(_lastHitX - 1, _lastHitY));
                if (_lastHitX < 9 && !_playerCells[_lastHitX + 1, _lastHitY].IsHit)
                    _possibleTargets.Add(Tuple.Create(_lastHitX + 1, _lastHitY));
            }
            else if (_currentDirection == Direction.Vertical)
            {
                if (_lastHitY > 0 && !_playerCells[_lastHitX, _lastHitY - 1].IsHit)
                    _possibleTargets.Add(Tuple.Create(_lastHitX, _lastHitY - 1));
                if (_lastHitY < 9 && !_playerCells[_lastHitX, _lastHitY + 1].IsHit)
                    _possibleTargets.Add(Tuple.Create(_lastHitX, _lastHitY + 1));
            }
        }

        private void DetermineDirection(int x, int y)
        {
            if ((x > 0 && _playerCells[x - 1, y].IsHit && _playerCells[x - 1, y].HasShip) ||
                (x < 9 && _playerCells[x + 1, y].IsHit && _playerCells[x + 1, y].HasShip))
            {
                _currentDirection = Direction.Horizontal;
                _directionFound = true;
            }
            else if ((y > 0 && _playerCells[x, y - 1].IsHit && _playerCells[x, y - 1].HasShip) ||
                     (y < 9 && _playerCells[x, y + 1].IsHit && _playerCells[x, y + 1].HasShip))
            {
                _currentDirection = Direction.Vertical;
                _directionFound = true;
            }
        }

        private void ReverseDirection()
        {
            if (_currentDirection == Direction.Horizontal)
            {
                _possibleTargets.RemoveAll(t => t.Item2 != _lastHitY);
            }
            else if (_currentDirection == Direction.Vertical)
            {
                _possibleTargets.RemoveAll(t => t.Item1 != _lastHitX);
            }
        }

        private bool IsShipSunk(Cell[,] cells, int x, int y)
        {
            for (int i = Math.Max(0, x - 1); i <= Math.Min(9, x + 1); i++)
            {
                for (int j = Math.Max(0, y - 1); j <= Math.Min(9, y + 1); j++)
                {
                    if (cells[i, j].HasShip && !cells[i, j].IsHit && (i == x || j == y))
                        return false;
                }
            }
            return true;
        }

        private void MarkSunkShip(Cell[,] cells, int x, int y, Brush color)
        {
            for (int i = Math.Max(0, x - 1); i <= Math.Min(9, x + 1); i++)
            {
                for (int j = Math.Max(0, y - 1); j <= Math.Min(9, y + 1); j++)
                {
                    if (cells[i, j].HasShip && (i == x || j == y))
                    {
                        cells[i, j].Color = color;
                    }
                }
            }
        }

        private void CheckGameOver()
        {
            if (_computerShipsRemaining == 0)
            {
                MessageBox.Show("Вы победили!", "Игра окончена");
                RestartButton.Visibility = Visibility.Visible;
            }
            else if (_playerShipsRemaining == 0)
            {
                MessageBox.Show("Компьютер победил!", "Игра окончена");
                RestartButton.Visibility = Visibility.Visible;
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGame();
        }
    }
}