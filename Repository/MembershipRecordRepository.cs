﻿using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;

namespace BeautyHubAPI.Repository
{
    public class MembershipRecordRepository : Repository<MembershipRecord>, IMembershipRecordRepository
    {
        private readonly ApplicationDbContext _context;
        public MembershipRecordRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<MembershipRecord> UpdateMembershipRecord(MembershipRecord entity)
        {
            _context.MembershipRecord.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
