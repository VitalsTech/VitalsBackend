using AutoMapper;
using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Application.Services
{
    public class AdminService : IAdminUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMultiProfileUserService _multiProfileUserService;
        private readonly IMapper _mapper;

        public AdminService(
            IUserRepository userRepository,
            IMultiProfileUserService multiProfileUserService,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _multiProfileUserService = multiProfileUserService;
            _mapper = mapper;
        }

        public async Task<SearchResult<UserDto>> SearchUsersAsync(UserSearchRequest request)
        {
            // Поиск по User (общие поля)
            var predicate = BuildSearchPredicate(request);
            var users = await _userRepository.GetPagedAsync(request.Page, request.PageSize, predicate);
            var totalCount = await _userRepository.CountAsync(predicate);

            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return new SearchResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<bool> SetUserStatusAsync(Guid publicId, UserStatusRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            user.IsActive = request.IsActive;
            user.UpdateTimestamp();
            _userRepository.UpdateUser(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BlockUserAsync(Guid publicId, UserBlockRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            user.BlockedUntil = request.BlockedUntil;
            user.BlockReason = request.BlockReason;
            user.IsActive = false;
            user.UpdateTimestamp();
            _userRepository.UpdateUser(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnblockUserAsync(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            user.BlockedUntil = null;
            user.BlockReason = null;
            user.IsActive = true;
            user.UpdateTimestamp();
            _userRepository.UpdateUser(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            user.IsActive = false;
            if (user.Email != null)
                user.Email = $"{user.Email}_deleted_{DateTime.UtcNow.Ticks}";
            user.UpdateTimestamp();
            _userRepository.UpdateUser(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreUserAsync(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            user.IsActive = true;
            user.UpdateTimestamp();
            _userRepository.UpdateUser(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        private System.Linq.Expressions.Expression<Func<User, bool>> BuildSearchPredicate(UserSearchRequest request)
        {
            System.Linq.Expressions.Expression<Func<User, bool>> predicate = u => true;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                predicate = predicate.AndAlso(u => u.PhoneNumber.Contains(request.PhoneNumber));

            if (!string.IsNullOrEmpty(request.Email))
                predicate = predicate.AndAlso(u => u.Email != null && u.Email.Contains(request.Email));

            if (!string.IsNullOrEmpty(request.FirstName))
                predicate = predicate.AndAlso(u => u.FirstName.Contains(request.FirstName));

            if (!string.IsNullOrEmpty(request.Surename))
                predicate = predicate.AndAlso(u => u.Surename.Contains(request.Surename));

            return predicate;
        }
    }
    public static class ExpressionExtensions
    {
        public static System.Linq.Expressions.Expression<Func<T, bool>> AndAlso<T>(
            this System.Linq.Expressions.Expression<Func<T, bool>> expr1,
            System.Linq.Expressions.Expression<Func<T, bool>> expr2)
        {
            var parameter = expr1.Parameters[0];
            var visitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
            var body2 = visitor.Visit(expr2.Body);
            var combined = System.Linq.Expressions.Expression.AndAlso(expr1.Body, body2);
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(combined, parameter);
        }

        private class ReplaceParameterVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            private readonly System.Linq.Expressions.ParameterExpression _oldParameter;
            private readonly System.Linq.Expressions.ParameterExpression _newParameter;

            public ReplaceParameterVisitor(System.Linq.Expressions.ParameterExpression oldParameter, System.Linq.Expressions.ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}