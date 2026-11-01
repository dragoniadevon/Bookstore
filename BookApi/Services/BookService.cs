using AutoMapper;
using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Services.Interface;

namespace Bookstore.Api.Services
{
    // Service содержит логику работы с книгами.
    // Получает DTO от Controller, работает с Entity через Repository
    // и возвращает DTO. О HTTP-кодах Service ничего не знает:
    // "не найдено" и "ошибка" он сообщает через null / DeleteResult.
    internal class BookService : IBaseService<BookDTO, BookPatchDTO>
    {
        // Repository для книг.
        private readonly IBaseRepository<Book> _repository;

        // Repository для авторов, жанров и издательств: нужны, чтобы проверять,
        // что AuthorId, GenreId и PublisherId из запроса существуют.
        // Без проверки база всё равно не дала бы сохранить книгу с несуществующим
        // автором (внешний ключ), но выдала бы непонятную ошибку 500 вместо 400.
        private readonly IBaseRepository<Author> _authorRepository;
        private readonly IBaseRepository<Genre> _genreRepository;
        private readonly IBaseRepository<Publisher> _publisherRepository;

        // AutoMapper: преобразование Entity <-> DTO.
        private readonly IMapper _mapper;

        // Все зависимости передаёт Dependency Injection.
        public BookService(
            IBaseRepository<Book> repository,
            IBaseRepository<Author> authorRepository,
            IBaseRepository<Genre> genreRepository,
            IBaseRepository<Publisher> publisherRepository,
            IMapper mapper)
        {
            _repository = repository;
            _authorRepository = authorRepository;
            _genreRepository = genreRepository;
            _publisherRepository = publisherRepository;
            _mapper = mapper;
        }

        // Получить все книги: Entity из БД -> список DTO.
        public async Task<IEnumerable<BookDTO>> GetAllAsync()
        {
            var books = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        // Получить книгу по ID. null = не найдена (Controller вернёт 404).
        public async Task<BookDTO?> GetByIdAsync(int id)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null)
            {
                return null;
            }

            return _mapper.Map<BookDTO>(book);
        }

        // Создать книгу.
        // null = указанный автор, жанр или издательство не существует (Controller вернёт 400).
        public async Task<BookDTO?> CreateAsync(BookDTO entity)
        {
            if (!await AuthorGenrePublisherExistAsync(entity.AuthorId, entity.GenreId, entity.PublisherId))
            {
                return null;
            }

            // DTO -> Entity
            var book = _mapper.Map<Book>(entity);
            book.Id = 0; // Id генерирует база, а не клиент

            var created = await _repository.CreateAsync(book);
            return _mapper.Map<BookDTO>(created);
        }

        // Полное обновление (PUT).
        // null = книга не найдена ИЛИ автор/жанр/издательство не существуют.
        public async Task<BookDTO?> UpdateAsync(BookDTO entity)
        {
            var book = await _repository.GetByIdAsync(entity.Id);
            if (book == null)
            {
                return null;
            }

            if (!await AuthorGenrePublisherExistAsync(entity.AuthorId, entity.GenreId, entity.PublisherId))
            {
                return null;
            }

            // Переносим значения из DTO в уже существующую Entity,
            // чтобы не потерять служебные поля из BaseEntity.
            _mapper.Map(entity, book);

            var updated = await _repository.UpdateAsync(book);
            return _mapper.Map<BookDTO>(updated);
        }

        // Частичное обновление (PATCH): меняем только переданные поля.
        // "Не передано" = null (все поля BookPatchDTO nullable).
        // Поэтому теперь можно, например, выставить остаток 0 или цену 0.
        public async Task<BookDTO?> PatchAsync(int id, BookPatchDTO dto)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null)
            {
                return null;
            }

            // Если меняется автор, жанр или издательство, сначала проверяем, что они есть.
            // "is int x" — проверка "не null" и сразу получение значения в переменную x.
            if (dto.AuthorId is int authorId)
            {
                if (await _authorRepository.GetByIdAsync(authorId) == null) return null;
                book.AuthorId = authorId;
            }

            if (dto.GenreId is int genreId)
            {
                if (await _genreRepository.GetByIdAsync(genreId) == null) return null;
                book.GenreId = genreId;
            }

            if (dto.PublisherId is int publisherId)
            {
                if (await _publisherRepository.GetByIdAsync(publisherId) == null) return null;
                book.PublisherId = publisherId;
            }

            // Остальные поля обновляем, только если они переданы.
            if (dto.Title != null) book.Title = dto.Title;
            if (dto.Description != null) book.Description = dto.Description;
            if (dto.Price is decimal price) book.Price = price;
            if (dto.StockQuantity is int stock) book.StockQuantity = stock;
            if (dto.ImageUrl != null) book.ImageUrl = dto.ImageUrl;

            var updated = await _repository.UpdateAsync(book);
            return _mapper.Map<BookDTO>(updated);
        }

        // Удалить книгу: Deleted, NotFound или InUse (книга есть в заказах).
        public async Task<DeleteResult> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Проверка: существуют ли автор, жанр и издательство с такими ID.
        // private — метод нужен только внутри этого класса, снаружи его не видно.
        // && проверяет условия по очереди и останавливается на первом false,
        // поэтому лишних запросов к базе не будет.
        private async Task<bool> AuthorGenrePublisherExistAsync(int authorId, int genreId, int publisherId)
        {
            return await _authorRepository.GetByIdAsync(authorId) != null
                && await _genreRepository.GetByIdAsync(genreId) != null
                && await _publisherRepository.GetByIdAsync(publisherId) != null;
        }
    }
}