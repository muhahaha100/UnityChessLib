using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityChess
{
    /// <summary>An 8x8 column-major matrix representation of a chessboard.</summary>
    public class Board
    {
        private int[] prev = new int[8];
        public King WhiteKing { get; private set; }
        public King BlackKing { get; private set; }
        private readonly Piece[,] boardMatrix;

        public Piece this[Square position]
        {
            get
            {
                if (position.IsValid) return boardMatrix[position.File - 1, position.Rank - 1];
                throw new ArgumentOutOfRangeException($"Position was out of range: {position}");
            }

            set
            {
                if (position.IsValid) boardMatrix[position.File - 1, position.Rank - 1] = value;
                else throw new ArgumentOutOfRangeException($"Position was out of range: {position}");
            }
        }

        public Piece this[int file, int rank]
        {
            get => this[new Square(file, rank)];
            set => this[new Square(file, rank)] = value;
        }

        /// <summary>Creates a Board with initial chess game position.</summary>
        public Board()
        {
            boardMatrix = new Piece[8, 8];
            SetStartingPosition();
        }

        /// <summary>Creates a deep copy of the passed Board.</summary>
        public Board(Board board)
        {
            // TODO optimize this method
            // Creates deep copy (makes copy of each piece and deep copy of their respective ValidMoves lists) of board (list of BasePiece's)
            // this may be a memory hog since each Board has a list of Piece's, and each piece has a list of Movement's
            // avg number turns/Board's per game should be around ~80. usual max number of pieces per board is 32
            boardMatrix = new Piece[8, 8];
            for (int file = 1; file <= 8; file++)
                for (int rank = 1; rank <= 8; rank++)
                {
                    Piece pieceToCopy = board[file, rank];
                    if (pieceToCopy == null) continue;

                    this[file, rank] = pieceToCopy.DeepCopy();
                }

            InitKings();
        }

        public void ClearBoard()
        {
            for (int file = 1; file <= 8; file++)
                for (int rank = 1; rank <= 8; rank++)
                    this[file, rank] = null;

            WhiteKing = null;
            BlackKing = null;
        }

        public void SetStartingPosition()
        {
            ClearBoard();


            //Row 2/Rank 7 and Row 7/Rank 2, both rows of pawns
            for (int file = 1; file <= 8; file++)
                foreach (int rank in new[] { 2, 7 })
                {
                    Square position = new Square(file, rank);
                    Side pawnColor = rank == 2 ? Side.White : Side.Black;
                    this[position] = new Pawn(position, pawnColor);
                }

            for (int i = 0; i < 8;)
            {
                int next = new System.Random().Next(8) + 1;
                if (!prev.Contains(next))
                {
                    prev[i] = next;
                    i++;
                }
            }
            RookRule();
            #region Kings Rule
            while ((prev[4] > prev[0] && prev[4] > prev[7]) || (prev[4] < prev[0] && prev[4] < prev[7]))
            {
                if ((prev[3] > prev[0] && prev[3] < prev[7]))
                {
                    int temp = prev[3];
                    prev[3] = prev[4];
                    prev[4] = temp;
                }
                else if ((prev[1] > prev[0] && prev[1] < prev[7]))
                {
                    int temp = prev[1];
                    prev[1] = prev[4];
                    prev[4] = temp;
                }
                else if ((prev[6] > prev[0] && prev[6] < prev[7]))
                {
                    int temp = prev[6];
                    prev[6] = prev[4];
                    prev[4] = temp;
                }
                else if ((prev[2] > prev[0] && prev[2] < prev[7]))
                {
                    int temp = prev[2];
                    prev[2] = prev[4];
                    prev[4] = temp;
                }
                else
                {
                    int temp = prev[5];
                    prev[5] = prev[4];
                    prev[4] = temp;
                }
            }
            #endregion
            #region Bishop Rule
            while ((prev[2] % 2 == 0 && prev[5] % 2 == 0))
            {
                if (prev[3] % 2 != 0)
                {
                    if (prev[2] > prev[5])
                    {
                        int temp = prev[2];
                        prev[2] = prev[3];
                        prev[3] = temp;
                    }
                    else
                    {
                        int temp = prev[5];
                        prev[5] = prev[3];
                        prev[3] = temp;
                    }
                }
                else if (prev[1] % 2 != 0)
                {
                    if (prev[2] > prev[5])
                    {
                        int temp = prev[2];
                        prev[2] = prev[1];
                        prev[1] = temp;
                    }
                    else
                    {
                        int temp = prev[5];
                        prev[5] = prev[1];
                        prev[1] = temp;
                    }
                }
                else
                {
                    if (prev[2] > prev[5])
                    {
                        int temp = prev[2];
                        prev[2] = prev[6];
                        prev[6] = temp;
                    }
                    else
                    {
                        int temp = prev[5];
                        prev[5] = prev[6];
                        prev[6] = temp;
                    }
                }
            }
            while (prev[2] % 2 == 1 && prev[5] % 2 == 1)
            {
                if (prev[3] % 2 != 1)
                {
                    if (prev[2] > prev[5])
                    {
                        int temp = prev[2];
                        prev[2] = prev[3];
                        prev[3] = temp;
                    }
                    else
                    {
                        int temp = prev[5];
                        prev[5] = prev[3];
                        prev[3] = temp;
                    }
                }
                else if (prev[1] % 2 != 1)
                {
                    if (prev[2] > prev[5])
                    {
                        int temp = prev[2];
                        prev[2] = prev[1];
                        prev[1] = temp;
                    }
                    else
                    {
                        int temp = prev[5];
                        prev[5] = prev[1];
                        prev[1] = temp;
                    }
                }
                else
                {
                    if (prev[2] > prev[5])
                    {
                        int temp = prev[2];
                        prev[2] = prev[6];
                        prev[6] = temp;
                    }
                    else
                    {
                        int temp = prev[5];
                        prev[5] = prev[6];
                        prev[6] = temp;
                    }
                }
            }
            #endregion
            //Rows 1 & 8/Ranks 8 & 1, back rows for both players
            for (int file = 1; file <= 8; file++)
                foreach (int rank in new[] { 1, 8 })
                {
                    Square position = new Square(file, rank);
                    Side pieceColor = rank == 1 ? Side.White : Side.Black;
                    if (file == prev[0] || file == prev[7])
                    {
                        this[position] = new Rook(position, pieceColor);
                    }
                    if (file == prev[1] || file == prev[6])
                    {
                        this[position] = new Knight(position, pieceColor);
                    }
                    if (file == prev[2] || file == prev[5])
                    {
                        this[position] = new Bishop(position, pieceColor);
                    }
                    if (file == prev[3])
                    {
                        this[position] = new Queen(position, pieceColor);
                    }
                    if (file == prev[4])
                    {
                        this[position] = new King(position, pieceColor);
                    }
                }

            WhiteKing = (King)this[prev[4], 1];
            BlackKing = (King)this[prev[4], 8];
        }

        public void MovePiece(Movement move)
        {
            if (!(this[move.Start] is Piece pieceToMove)) throw new ArgumentException($"No piece was found at the given position: {move.Start}");

            this[move.Start] = null;
            this[move.End] = pieceToMove;

            pieceToMove.HasMoved = true;
            pieceToMove.Position = move.End;

            (move as SpecialMove)?.HandleAssociatedPiece(this);
        }

        internal bool IsOccupied(Square position) => this[position] != null;

        internal bool IsOccupiedBySide(Square position, Side side) => this[position] is Piece piece && piece.Color == side;

        public void InitKings()
        {
            for (int file = 1; file <= 8; file++)
            {
                for (int rank = 1; rank <= 8; rank++)
                {
                    if (this[file, rank] is King king)
                    {
                        if (king.Color == Side.White) WhiteKing = king;
                        else BlackKing = king;
                    }
                }
            }
        }

        private void RookRule()
        {
            while ((prev[0] - prev[7]) == 1 || (prev[0] - prev[7]) == -1)
            {
                if (prev[7] > prev[0])
                {
                    int temp = prev[7];
                    prev[7] = prev[4];
                    prev[4] = temp;
                }
                else
                {
                    int temp = prev[0];
                    prev[0] = prev[4];
                    prev[4] = temp;
                }
            }
            if (prev[0] > prev[7])
            {
                int temp = prev[0];
                prev[0] = prev[7];
                prev[7] = temp;
            }
        }
    }
}