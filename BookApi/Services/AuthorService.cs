using AutoMapper;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Services.Interface;

namespace Bookstore.Api.Services
{
    // Service содержит основную логику работы с авторами.
    //
    // Controller отвечает за HTTP-запрос и HTTP-ответ,
    // а Service отвечает за выполнение самой операции:
    //
    // Controller
    //     ↓
    // Service
    //     ↓
    // Repository
    //     ↓
    // Database
    //
    // Service не должен знать о HTTP-кодах:
    // Ok(), NotFound(), BadRequest() и т.д.
    internal class AuthorService : IBaseService<AuthorDTO>
    {
        // Repository отвечает за работу с базой данных.
        //
        // Service не обращается напрямую к Entity Framework/DbContext,
        // а использует Repository как отдельный слой доступа к данным.
        private readonly IBaseRepository<Author> _repository;

        // AutoMapper используется для преобразования объектов
        // между Entity и DTO.
        //
        // Например:
        //
        // Author Entity → AuthorDTO
        // AuthorDTO → Author Entity
        private readonly IMapper _mapper;


        // Dependency Injection передаёт Service необходимые зависимости.
        //
        // Мы не создаём Repository и Mapper через new.
        // ASP.NET Core сам создаёт их и передаёт сюда.
        public AuthorService(
            IBaseRepository<Author> repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }


        // Создание нового автора.
        //
        // Controller передаёт сюда DTO,
        // Service преобразует DTO в Entity,
        // Repository сохраняет Entity в БД,
        // после чего Service возвращает DTO.
        public async Task<AuthorDTO?> CreateAsync(AuthorDTO entity)
        {
            // AuthorDTO → Author Entity
            //
            // DTO используется на границе API,
            // а Entity используется для работы с базой данных.
            var authorEntity = _mapper.Map<Author>(entity);

            // Передаём Entity в Repository для сохранения в БД.
            var createdAuthor = await _repository.CreateAsync(authorEntity);

            // Author Entity → AuthorDTO
            //
            // Возвращаем наружу DTO, а не Entity.
            return _mapper.Map<AuthorDTO>(createdAuthor);
        }


        // Удаление автора по ID.
        //
        // Service получает только ID и передаёт его Repository.
        public async Task<bool> DeleteAsync(int id)
        {
            // Repository возвращает true, если удаление прошло успешно,
            // и false, если удалить автора не получилось.
            return await _repository.DeleteAsync(id);
        }


        // Получение всех авторов.
        public async Task<IEnumerable<AuthorDTO>> GetAllAsync()
        {
            // Получаем Entity из базы данных.
            var authors = await _repository.GetAllAsync();

            // Преобразуем коллекцию Entity в коллекцию DTO.
            //
            // Например:
            //
            // IEnumerable<Author>
            //        ↓
            // IEnumerable<AuthorDTO>
            return _mapper.Map<IEnumerable<AuthorDTO>>(authors);
        }


        // Получение одного автора по ID.
        public async Task<AuthorDTO?> GetByIdAsync(int id)
        {
            // Ищем автора в базе данных.
            var author = await _repository.GetByIdAsync(id);

            // Если автора нет, возвращаем null.
            //
            // Controller позже превратит это в:
            //
            // HTTP 404 Not Found
            if (author == null)
            {
                return null;
            }

            // Если автор найден,
            // преобразуем Entity в DTO.
            return _mapper.Map<AuthorDTO>(author);
        }


        // Полное обновление автора.
        //
        // PUT обычно означает замену/полное обновление ресурса.
        public async Task<AuthorDTO?> UpdateAsync(AuthorDTO entity)
        {
            // Ищем существующего автора.
            //
            // ID находится внутри DTO:
            //
            // entity.Id
            //
            // Поэтому здесь используем entity.Id.
            var author = await _repository.GetByIdAsync(entity.Id);

            // Если такого автора нет,
            // сообщаем Controller через null.
            //
            // Controller уже решит,
            // что нужно вернуть HTTP 404.
            if (author == null)
            {
                return null;
            }

            // DTO преобразуется обратно в Entity.
            //
            // В данном случае мы передаём Entity в Repository
            // для полного обновления.
            var updatedAuthor = await _repository.UpdateAsync(
                _mapper.Map<Author>(entity));

            // Возвращаем результат обновления в виде DTO.
            return _mapper.Map<AuthorDTO>(updatedAuthor);
        }


        // Частичное обновление автора.
        //
        // PATCH отличается от PUT тем,
        // что мы изменяем только те свойства,
        // которые были переданы клиентом.
        public async Task<AuthorDTO?> PatchAsync(
            int id,
            AuthorDTO authorDTO)
        {
            // Сначала получаем существующего автора.
            //
            // Это важно для PATCH:
            // нам нужен текущий объект,
            // чтобы изменить только отдельные его свойства.
            var author = await _repository.GetByIdAsync(id);

            // Если автора нет — возвращаем null.
            //
            // Controller превратит это в HTTP 404.
            if (author == null)
            {
                return null;
            }

            // Проверяем, было ли передано новое имя.
            //
            // Если Name == null, значит в нашей упрощённой
            // реализации PATCH имя менять не нужно.
            if (authorDTO.Name != null)
            {
                author.Name = authorDTO.Name;
            }

            // Сохраняем уже изменённую Entity.
            //
            // Обрати внимание:
            // здесь мы не создаём новую Entity через AutoMapper.
            //
            // Мы взяли существующую Entity из БД,
            // изменили нужное свойство
            // и передали её обратно в Repository.
            var updatedAuthor = await _repository.UpdateAsync(author);

            // Возвращаем обновлённого автора как DTO.
            return _mapper.Map<AuthorDTO>(updatedAuthor);
        }
    }
}