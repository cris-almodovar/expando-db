using System;

namespace ExpandoDB.Tests
{
    public static class TestUtils
    {
        public static Content CreateBook(string title, string author, DateTime publishDate, int rating, string description)
        {
            var content = new Content();
            content._id = Guid.NewGuid();
            content._createdTimestamp = DateTime.UtcNow;

            dynamic book = content;
            book.Title = title;
            book.Author = author;
            book.PublishDate = publishDate;
            book.Rating = rating;
            book.Description = description;

            return content;
        }
    }
}
