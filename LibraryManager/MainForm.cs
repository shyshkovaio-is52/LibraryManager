using LibraryManager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryManager
{
    public enum BookStatus { Available, Borrowed, Archived }
    public class Book
    {
        private int _id;
        private string _title;
        private string _author;
        private BookStatus _status;

        public int Id => _id;
        public string Title => _title;
        public BookStatus Status
        {
            get => _status;
            set => _status = value;
        }

        public Book(int id, string title, string author)
        {
            _id = id;
            _title = title;
            _author = author;
            _status = BookStatus.Available;
        }
    }

    public class Reader
    {
        private int _ticketNumber;
        private string _fullName;
        private float _unpaidFines;

        public int TicketNumber => _ticketNumber;
        public string FullName => _fullName;

        public Reader(int ticket, string name)
        {
            _ticketNumber = ticket;
            _fullName = name;
            _unpaidFines = 0.0f;
        }

        public bool CanBorrow()
        {
            return _unpaidFines <= 0;
        }
    }

    public class Loan
    {
        private int _bookId;
        private int _readerTicket;
        private DateTime _issueDate;

        public Loan(int bookId, int ticket)
        {
            _bookId = bookId;
            _readerTicket = ticket;
            _issueDate = DateTime.Now;
        }
    }
    public partial class MainForm : Form
    {
  
        private List<Book> _books = new List<Book>();
        private List<Reader> _readers = new List<Reader>();
        private List<Loan> _loans = new List<Loan>();

        public MainForm()
        {
            InitializeData();      
            InitializeInterface(); 
        }

        private void InitializeData()
        {
            
            _books.Add(new Book(1, "Гаррі Поттер", "Джоан Роулінг"));
            _books.Add(new Book(2, "Віднесені вітром", "Марґарет Мітчелл"));

            _readers.Add(new Reader(88, "Олександр Донець"));
        }

        private void InitializeInterface()
        {
            this.Text = "Library Manager — Робоче місце бібліотекаря";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblBook = new Label { Text = "ID Книги (1 або 2):", Location = new Point(30, 30), AutoSize = true };
            TextBox txtBook = new TextBox { Location = new Point(150, 30), Width = 150 };

            Label lblReader = new Label { Text = "№ Квитка (88):", Location = new Point(30, 70), AutoSize = true };
            TextBox txtReader = new TextBox { Location = new Point(150, 70), Width = 150 };

            Button btnIssue = new Button
            {
                Text = "Оформити видачу",
                Location = new Point(130, 130),
                Size = new Size(130, 40),
                BackColor = Color.LightPink
            };

          
            btnIssue.Click += (s, e) => {
                if (int.TryParse(txtBook.Text, out int bId) && int.TryParse(txtReader.Text, out int rId))
                {
                   
                    Book foundBook = null;
                    foreach (Book b in _books)
                    {
                        if (b.Id == bId)
                        {
                            foundBook = b;
                            break;
                        }
                    }

        
                    Reader foundReader = null;
                    foreach (Reader r in _readers)
                    {
                        if (r.TicketNumber == rId)
                        {
                            foundReader = r;
                            break;
                        }
                    }

                    
                    if (foundBook != null && foundReader != null)
                    {
                        if (foundBook.Status == BookStatus.Available && foundReader.CanBorrow())
                        {
                            foundBook.Status = BookStatus.Borrowed; 
                            _loans.Add(new Loan(bId, rId));         

                            MessageBox.Show($"Успіх! Книга '{foundBook.Title}' видана {foundReader.FullName}.", "Бібліотека");
                        }
                        else
                        {
                            string msg = foundBook.Status != BookStatus.Available ? "Книга вже на руках." : "У читача є борги.";
                            MessageBox.Show(msg, "Відмова");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Книгу або читача не знайдено", "Помилка даних.");
                    }
                }
                else
                {
                    MessageBox.Show("Будь ласка, введіть числові значення.", "Помилка вводу");
                }
            };

        
            this.Controls.Add(lblBook);
            this.Controls.Add(txtBook);
            this.Controls.Add(lblReader);
            this.Controls.Add(txtReader);
            this.Controls.Add(btnIssue);
        }
    }
}

