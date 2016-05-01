using System;

namespace ExpandoDB.Tests
{
    public static class TestUtils
    {
        public static Document CreateBook(string title, string author, DateTime? publishDate, int? rating, string description)
        {
            var document = new Document();
            document._id = Guid.NewGuid();
            document._createdTimestamp = DateTime.UtcNow;

            dynamic book = document;
            book.Title = title;
            book.Author = author;
            book.PublishDate = publishDate;
            book.Rating = rating;
            book.Description = description;

            return document;
        }
    }
}
