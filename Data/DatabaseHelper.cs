using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QrCode.Models;

namespace QrCode.Data.QuickQR.Data
{
    public class DatabaseHelper
    {
        private readonly string _conn;
        public DatabaseHelper(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection");
        }

        public int AddQRCode(string content, string contentType, string fileName, string format,
                             string foreColor, string backColor, bool hasLogo, string logoFileName)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = new SqlCommand("AddQRCode", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Content", content);
            cmd.Parameters.AddWithValue("@ContentType", (object)contentType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FileName", (object)fileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Format", (object)format ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ForeColor", (object)foreColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BackColor", (object)backColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HasLogo", hasLogo);
            cmd.Parameters.AddWithValue("@LogoFileName", (object)logoFileName ?? DBNull.Value);

            con.Open();
            var id = cmd.ExecuteScalar();
            return id == null ? 0 : Convert.ToInt32(id);
        }

        public List<QRCodeModel> GetAllQRCodes()
        {
            var list = new List<QRCodeModel>();
            using var con = new SqlConnection(_conn);
            using var cmd = new SqlCommand("GetAllQRCodes", con);
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new QRCodeModel
                {
                    Id = Convert.ToInt32(r["Id"]),
                    Content = r["Content"].ToString(),
                    ContentType = r["ContentType"]?.ToString(),
                    FileName = r["FileName"]?.ToString(),
                    Format = r["Format"]?.ToString(),
                    ForeColor = r["ForeColor"]?.ToString(),
                    BackColor = r["BackColor"]?.ToString(),
                    HasLogo = r["HasLogo"] != DBNull.Value && Convert.ToBoolean(r["HasLogo"]),
                    LogoFileName = r["LogoFileName"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(r["CreatedAt"])
                });
            }
            return list;
        }

        public QRCodeModel GetQRCodeById(int id)
        {
            using var con = new SqlConnection(_conn);
            using var cmd = new SqlCommand("GetQRCodeById", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", id);
            con.Open();
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return new QRCodeModel
                {
                    Id = Convert.ToInt32(r["Id"]),
                    Content = r["Content"].ToString(),
                    ContentType = r["ContentType"]?.ToString(),
                    FileName = r["FileName"]?.ToString(),
                    Format = r["Format"]?.ToString(),
                    ForeColor = r["ForeColor"]?.ToString(),
                    BackColor = r["BackColor"]?.ToString(),
                    HasLogo = r["HasLogo"] != DBNull.Value && Convert.ToBoolean(r["HasLogo"]),
                    LogoFileName = r["LogoFileName"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(r["CreatedAt"])
                };
            }
            return null;
        }
    }
}
