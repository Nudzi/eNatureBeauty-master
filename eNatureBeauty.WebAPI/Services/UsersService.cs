﻿using AutoMapper;
using eNatureBeauty.Model.Requests;
using eNatureBeauty.WebAPI.Database;
using eNatureBeauty.WebAPI.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace eNatureBeauty.WebAPI.Services
{
    public class UsersService : IUsersService
    {
        private readonly natureBeautyContext _context;
        private readonly IMapper _mapper;
        public UsersService(natureBeautyContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public Model.Users Authentication(string username, string pass)
        {
            var user = _context.Users.FirstOrDefault(x => x.UserName == username);

            if (user != null)
            {
                var hashedPass = GenerateHash(user.PasswordSalt, pass);

                if (hashedPass == user.PasswordHash)
                {
                    var userTypes = _context.UsersUserTypes.Where(x => x.UserId == user.Id);
                    Model.Users newUser = new Model.Users();

                    foreach (var item in userTypes)
                    {

                        newUser.UserTypes = new List<Model.UsersUserTypes>();
                        newUser.UserTypes.Add(new Model.UsersUserTypes
                        {
                            IsActive = true,
                            UserId = item.UserId,
                            UserTypeId = item.UserTypeId,
                            ModifiedDate = item.ModifiedDate
                        });
                    }
                    newUser.FirstName = user.FirstName;
                    newUser.LastName = user.LastName;
                    newUser.UserName = user.UserName;
                    newUser.Email = user.Email;
                    newUser.Id = user.Id;
                    newUser.Telephone = user.Telephone;

                    return newUser;

                }
            }

            return null;
        }
        public List<Model.Users> Get(UsersSearchRequest request)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(request?.FirstName))
            {
                query = query.Where(x => x.FirstName.StartsWith(request.FirstName));
            }
            if (!string.IsNullOrWhiteSpace(request?.LastName))
            {
                query = query.Where(x => x.LastName.StartsWith(request.LastName));
            }
            var list = query.ToList();
            return _mapper.Map<List<Model.Users>>(list);
        }

        public Model.Users GetById(int id)
        {
            var entity = _context.Users.Find(id);

            return _mapper.Map<Model.Users>(entity);
        }

        public Model.Users Insert(UsersInsertRequest request)
        {
            var entity = _mapper.Map<Database.Users>(request);

            if (request.Password != request.PasswordConfirmation)
            {
                throw new UserException("Passwords don't match!");
            }

            entity.PasswordSalt = GenerateSalt();
            entity.PasswordHash = GenerateHash(entity.PasswordSalt, request.Password);

            _context.Users.Add(entity);
            _context.SaveChanges();

            foreach (var userTypes in request.UserTypes)
            {
                _context.UsersUserTypes.Add(new Database.UsersUserTypes()
                {
                    UserId = entity.Id,
                    UserTypeId = userTypes,
                    ModifiedDate = DateTime.Now,
                    IsActive = true
                });
            }

            _context.SaveChanges();
            return _mapper.Map<Model.Users>(entity);
        }
        public Model.Users Update(int id, UsersInsertRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Password)) //check pass, if passe exist
            {
                if (request.Password != request.PasswordConfirmation) //check similarity
                    throw new Exception("Password is not the same!");
            }
            var entity = _context.Users.Find(id);
            entity.PasswordSalt = GenerateSalt();
            entity.PasswordHash = GenerateHash(entity.PasswordSalt, request.Password);

            foreach (var userTypes in request.UserTypes)
            {
                _context.UsersUserTypes.Add(new Database.UsersUserTypes()
                {
                    UserId = entity.Id,
                    UserTypeId = userTypes,
                    ModifiedDate = DateTime.Now,
                    IsActive = true
                });
            }
            _context.Users.Attach(entity);
            _context.Users.Update(entity);

            _mapper.Map(request, entity);
            _context.SaveChanges();

            _context.SaveChanges(); //save it
            return _mapper.Map<Model.Users>(entity);//return our model, than go to controller
        }



        public static string GenerateSalt()
        {
            var buf = new byte[16];
            (new RNGCryptoServiceProvider()).GetBytes(buf);//raandom number based on our cores and time...
            return Convert.ToBase64String(buf);//change it to 64base because it is the most simply converter bytes to string
        }
        public static string GenerateHash(string salt, string password)
        {
            byte[] src = Convert.FromBase64String(salt);
            byte[] bytes = Encoding.Unicode.GetBytes(password);
            byte[] dst = new byte[src.Length + bytes.Length];

            System.Buffer.BlockCopy(src, 0, dst, 0, src.Length);
            System.Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);

            HashAlgorithm algorithm = HashAlgorithm.Create("SHA1");//sha512
            byte[] inArray = algorithm.ComputeHash(dst);
            return Convert.ToBase64String(inArray);
        }
    }
}
