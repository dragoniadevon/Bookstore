using AutoMapper;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Models.Entity;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Services.Interface;

namespace Bookstore.Api.Services
{
    // Service содержит основную логику работы с издательствами.
    //
    // Controller → Service → Repository → Database
    //
    // Service не должен знать о HTTP-кодах:
    // Ok(), NotFound(), BadRequest() и т.д.
    internal class PublisherService : IBaseService<PublisherDTO>
    {
        // Repository отвечает за работу с базой данных.
        private readonly IBaseRepository<Publisher> _repository;

        // AutoMapper преобразует объекты между Entity и DTO.
        private readonly IMapper _mapper;

        // Dependency Injection передаёт Service необходимые зависимости.
        public PublisherService(
            IBaseRepository<Publisher> repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // Создание нового издательства.
        // DTO → Entity → Repository → Entity → DTO.
        public async Task<PublisherDTO?> CreateAsync(PublisherDTO entity)
        {
            var publisherEntity = _mapper.Map<Publisher>(entity);
            var createdPublisher = await _repository.CreateAsync(publisherEntity);
            return _mapper.Map<PublisherDTO>(createdPublisher);
        }

        // Удаление издательства по ID.
        // Repository возвращает true, если удаление прошло успешно.
        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Получение всех издательств.
        public async Task<IEnumerable<PublisherDTO>> GetAllAsync()
        {
            var publishers = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<PublisherDTO>>(publishers);
        }

        // Получение одного издательства по ID.
        // Если не найдено, возвращаем null (Controller превратит это в 404).
        public async Task<PublisherDTO?> GetByIdAsync(int id)
        {
            var publisher = await _repository.GetByIdAsync(id);

            if (publisher == null)
            {
                return null;
            }

            return _mapper.Map<PublisherDTO>(publisher);
        }

        // Полное обновление издательства (PUT).
        public async Task<PublisherDTO?> UpdateAsync(PublisherDTO entity)
        {
            // Ищем существующее издательство. ID находится внутри DTO.
            var publisher = await _repository.GetByIdAsync(entity.Id);

            // Если такого нет, сообщаем Controller через null (будет 404).
            if (publisher == null)
            {
                return null;
            }

            var updatedPublisher = await _repository.UpdateAsync(
                _mapper.Map<Publisher>(entity));

            return _mapper.Map<PublisherDTO>(updatedPublisher);
        }

        // Частичное обновление издательства (PATCH).
        // Меняем только те свойства, которые были переданы клиентом.
        public async Task<PublisherDTO?> PatchAsync(
            int id,
            PublisherDTO publisherDTO)
        {
            var publisher = await _repository.GetByIdAsync(id);

            if (publisher == null)
            {
                return null;
            }

            // Если Name передан, меняем его.
            if (publisherDTO.Name != null)
            {
                publisher.Name = publisherDTO.Name;
            }

            // Если Description передано, меняем его.
            if (publisherDTO.Description != null)
            {
                publisher.Description = publisherDTO.Description;
            }

            var updatedPublisher = await _repository.UpdateAsync(publisher);

            return _mapper.Map<PublisherDTO>(updatedPublisher);
        }
    }
}