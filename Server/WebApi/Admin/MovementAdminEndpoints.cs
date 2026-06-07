using System.Security.Claims;
using System.Text.Json.Serialization;
using Library.SystemModels;
using Server.Envir;
using Server.WebApi.Auth;

namespace Server.WebApi.Admin
{
    public static class MovementAdminEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/admin/movements")
                .RequireAuthorization(p => p.RequireRole("Admin", "SuperAdmin"));

            group.MapGet("/", () =>
            {
                var dbList = SEnvir.MovementInfoList?.Binding;
                int dbCount = dbList?.Count ?? -1;

                if (dbList == null || dbList.Count == 0)
                {
                    // 数据库无记录，尝试从已加载地图单元格中收集
                    var cellMovements = new HashSet<MovementInfo>();
                    foreach (var kv in SEnvir.Maps)
                    {
                        var map = kv.Value;
                        if (map.Cells == null) continue;
                        for (int x = 0; x < map.Width; x++)
                            for (int y = 0; y < map.Height; y++)
                            {
                                var cell = map.Cells[x, y];
                                if (cell?.Movements == null) continue;
                                foreach (var m in cell.Movements)
                                    cellMovements.Add(m);
                            }
                    }

                    if (cellMovements.Count > 0)
                    {
                        var result = cellMovements.Select(m => new
                        {
                            index = m.Index,
                            sourceMap = m.SourceRegion?.Map?.Description ?? "",
                            sourceRegion = m.SourceRegion?.ServerDescription ?? "",
                            sourceX = m.SourceRegion?.PointList?.FirstOrDefault().X ?? 0,
                            sourceY = m.SourceRegion?.PointList?.FirstOrDefault().Y ?? 0,
                            destMap = m.DestinationRegion?.Map?.Description ?? "",
                            destRegion = m.DestinationRegion?.ServerDescription ?? "",
                            destX = m.DestinationRegion?.PointList?.FirstOrDefault().X ?? 0,
                            destY = m.DestinationRegion?.PointList?.FirstOrDefault().Y ?? 0
                        }).ToList();
                        return Results.Ok(new { success = true, movements = result, source = "cells", count = result.Count });
                    }

                    return Results.Ok(new { success = true, movements = Array.Empty<object>(), source = "empty", count = 0, dbCount = dbCount });
                }

                var items = dbList.Select(m => new
                {
                    index = m.Index,
                    sourceMap = m.SourceRegion?.Map?.Description ?? "",
                    sourceRegion = m.SourceRegion?.ServerDescription ?? "",
                    sourceX = m.SourceRegion?.PointList?.FirstOrDefault().X ?? 0,
                    sourceY = m.SourceRegion?.PointList?.FirstOrDefault().Y ?? 0,
                    destMap = m.DestinationRegion?.Map?.Description ?? "",
                    destRegion = m.DestinationRegion?.ServerDescription ?? "",
                    destX = m.DestinationRegion?.PointList?.FirstOrDefault().X ?? 0,
                    destY = m.DestinationRegion?.PointList?.FirstOrDefault().Y ?? 0
                }).ToList();

                return Results.Ok(new { success = true, movements = items, source = "database", count = items.Count });
            });

            group.MapPut("/{id:int}", (int id, UpdateMovementRequest req) =>
            {
                var m = SEnvir.MovementInfoList?.Binding?.FirstOrDefault(x => x.Index == id);
                if (m == null) return Results.Json(new { success = false, message = "连接点不存在" }, statusCode: 404);

                if (m.SourceRegion != null && m.SourceRegion.PointList.Count > 0)
                {
                    var pt = m.SourceRegion.PointList[0];
                    m.SourceRegion.PointList[0] = new System.Drawing.Point(
                        req.SourceX > 0 ? req.SourceX : pt.X,
                        req.SourceY > 0 ? req.SourceY : pt.Y);
                }
                if (m.DestinationRegion != null && m.DestinationRegion.PointList.Count > 0)
                {
                    var pt = m.DestinationRegion.PointList[0];
                    m.DestinationRegion.PointList[0] = new System.Drawing.Point(
                        req.DestX > 0 ? req.DestX : pt.X,
                        req.DestY > 0 ? req.DestY : pt.Y);
                }

                SEnvir.Log($"[AdminAPI] 连接点 #{id} 已更新");
                return Results.Ok(new { success = true, message = "保存成功" });
            });

            group.MapDelete("/{id:int}", (int id) =>
            {
                var m = SEnvir.MovementInfoList?.Binding?.FirstOrDefault(x => x.Index == id);
                if (m == null) return Results.Json(new { success = false, message = "连接点不存在" }, statusCode: 404);
                SEnvir.MovementInfoList?.Binding?.Remove(m);
                SEnvir.Log($"[AdminAPI] 连接点 #{id} 已删除");
                return Results.Ok(new { success = true, message = "已删除" });
            });
        }
    }

    public class UpdateMovementRequest
    {
        [JsonPropertyName("sourceX")] public int SourceX { get; set; }
        [JsonPropertyName("sourceY")] public int SourceY { get; set; }
        [JsonPropertyName("destX")] public int DestX { get; set; }
        [JsonPropertyName("destY")] public int DestY { get; set; }
    }
}
