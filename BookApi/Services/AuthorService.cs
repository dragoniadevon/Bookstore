using AutoMapper;
using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;
using Bookstore.Api.Repositories;
using Bookstore.Api.Repositories.Interface;
using Bookstore.Api.Services.Interface;

namespace Bookstore.Api.Services
{
    internal class AuthorService : IBaseService<AuthorDTO>
    {
        private readonly IBaseRepository<Author> _repository;
        private readonly IMapper _mapper;
        public AuthorService(IBaseRepository<Author> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<AuthorDTO> CreateAsync(AuthorDTO entity)
        {
            var obj = await _repository.CreateAsync(_mapper.Map<Author>(entity));
            return _mapper.Map<AuthorDTO>(obj);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<AuthorDTO>> GetAllAsync()
        {
            return _mapper.Map<IEnumerable<AuthorDTO>>(await _repository.GetAllAsync());
        }

        public async Task<AuthorDTO> GetByIdAsync(int id)
        {
            return _mapper.Map<AuthorDTO>(_repository.GetByIdAsync(id));
        }

        public async Task<AuthorDTO> UpdateAsync(AuthorDTO entity)
        {
            var obj = await _repository.UpdateAsync(_mapper.Map<Author>(entity));
            return _mapper.Map<AuthorDTO>(obj);
        }
    }
}