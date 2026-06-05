using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;
using Server.Envir;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Game data API endpoints
    /// </summary>
    public static class GameDataEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/game")
                .RequireAuthorization();

            // Items
            group.MapGet("/items", GetItems);
            group.MapGet("/items/types", GetItemTypes);
            group.MapGet("/items/required-types", GetRequiredTypes);
            group.MapGet("/items/required-classes", GetRequiredClasses);
            group.MapGet("/items/rarities", GetRarities);
            group.MapGet("/items/{index:int}", GetItemDetail);
            group.MapGet("/items/{index:int}/drops", GetItemDrops);
            group.MapPost("/items", CreateItem);
            group.MapPut("/items/{index:int}", UpdateItem);
            group.MapPost("/items/give", GiveItem);

            // Maps
            group.MapGet("/maps", GetMaps);
            group.MapGet("/maps/{index:int}", GetMapDetail);
            group.MapGet("/maps/{index:int}/respawns", GetMapRespawns);
            group.MapPost("/maps/{index:int}/respawns", AddMapRespawn);
            group.MapPut("/maps/{index:int}/respawns/{respawnId:int}", UpdateMapRespawn);
            group.MapDelete("/maps/{index:int}/respawns/{respawnId:int}", DeleteMapRespawn);
            group.MapPut("/maps/{index:int}", UpdateMap);
            group.MapPost("/maps/teleport", TeleportPlayer);
            group.MapPost("/maps/{index:int}/clear-monsters", ClearMonstersOnMap);
            group.MapGet("/maps/{index:int}/movements", GetMapMovements);
            group.MapPost("/maps/{index:int}/movements", AddMapMovement);
            group.MapPut("/maps/{index:int}/movements/{movementId:int}", UpdateMapMovement);
            group.MapDelete("/maps/{index:int}/movements/{movementId:int}", DeleteMapMovement);

            // Monsters
            group.MapGet("/monsters", GetMonsters);
            group.MapGet("/monsters/{index:int}", GetMonsterDetail);
            group.MapGet("/monsters/{index:int}/drops", GetMonsterDrops);
            group.MapPost("/monsters/{index:int}/drops", AddMonsterDrop);
            group.MapPut("/monsters/{index:int}/drops/{dropId:int}", UpdateMonsterDrop);
            group.MapDelete("/monsters/{index:int}/drops/{dropId:int}", DeleteMonsterDrop);
            group.MapPut("/monsters/{index:int}", UpdateMonster);
            group.MapPost("/monsters/{index:int}/clear", ClearMonstersByType);
            group.MapPost("/monsters/spawn", SpawnMonsterNearPlayer);

            // Other game data
            group.MapGet("/npcs", GetNpcs);
            group.MapGet("/npcs/{index:int}", GetNpcDetail);
            group.MapPut("/npcs/{index:int}", UpdateNpc);
            group.MapGet("/mapregions", GetMapRegions);
            group.MapGet("/mapregions/{index:int}", GetMapRegionDetail);
            group.MapPut("/mapregions/{index:int}", UpdateMapRegionPoints);

            group.MapGet("/magics", GetMagics);
            group.MapGet("/magics/{index:int}", GetMagicDetail);
            group.MapPut("/magics/{index:int}", UpdateMagic);
            group.MapPost("/magics/grant", GrantMagic);

            group.MapGet("/classes", GetClasses);

            // Base Stats
            group.MapGet("/basestats", GetBaseStats);
            group.MapGet("/basestats/{index:int}", GetBaseStat);
            group.MapPost("/basestats", CreateBaseStat);
            group.MapPut("/basestats/{index:int}", UpdateBaseStat);
            group.MapDelete("/basestats/{index:int}", DeleteBaseStat);

            // Quests
            group.MapGet("/quests", GetQuests);
            group.MapGet("/quests/{index:int}", GetQuestDetail);
            group.MapPost("/quests", CreateQuest);
            group.MapPut("/quests/{index:int}", UpdateQuest);
            group.MapDelete("/quests/{index:int}", DeleteQuest);

            // Store
            group.MapGet("/store", GetStoreItems);
            group.MapGet("/store/{index:int}", GetStoreItemDetail);
            group.MapPost("/store", AddStoreItem);
            group.MapPut("/store/{index:int}", UpdateStoreItem);
            group.MapDelete("/store/{index:int}", DeleteStoreItem);
        }

        /// <summary>
        /// Get items list with pagination
        /// </summary>
        private static IResult GetItems(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int page = 1,
            int pageSize = 50,
            string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var (items, total) = dataService.GetItems(page, pageSize, search);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                items
            });
        }

        /// <summary>
        /// Get item detail by index
        /// </summary>
        private static IResult GetItemDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var item = dataService.GetItemDetail(index);
            if (item == null)
            {
                return Results.NotFound(new { message = "物品不存在" });
            }

            return Results.Ok(item);
        }

        /// <summary>
        /// Create new item
        /// </summary>
        private static IResult CreateItem(
            ClaimsPrincipal user,
            ServerDataService dataService,
            AddItemRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (request.Index <= 0)
            {
                return Results.BadRequest(new { message = "物品ID必须大于0" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "物品名称不能为空" });
            }

            var (success, message, item) = dataService.CreateItem(request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, item });
        }

        /// <summary>
        /// Update item by index
        /// </summary>
        private static IResult UpdateItem(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateItemRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateItem(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Give item to character
        /// </summary>
        private static IResult GiveItem(
            ClaimsPrincipal user,
            ServerDataService dataService,
            GiveItemRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.CharacterName))
            {
                return Results.BadRequest(new { message = "角色名称不能为空" });
            }

            if (request.ItemIndex <= 0)
            {
                return Results.BadRequest(new { message = "物品ID无效" });
            }

            if (request.Count <= 0)
            {
                return Results.BadRequest(new { message = "数量必须大于0" });
            }

            var (success, message) = dataService.GiveItemToCharacter(request.CharacterName, request.ItemIndex, request.Count);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Get all item types
        /// </summary>
        private static IResult GetItemTypes(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var types = dataService.GetItemTypes();
            return Results.Ok(types);
        }

        /// <summary>
        /// Get all required types
        /// </summary>
        private static IResult GetRequiredTypes(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var types = dataService.GetRequiredTypes();
            return Results.Ok(types);
        }

        /// <summary>
        /// Get all required classes
        /// </summary>
        private static IResult GetRequiredClasses(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var classes = dataService.GetRequiredClasses();
            return Results.Ok(classes);
        }

        /// <summary>
        /// Get all rarity types
        /// </summary>
        private static IResult GetRarities(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var rarities = dataService.GetRarities();
            return Results.Ok(rarities);
        }

        /// <summary>
        /// Get maps list
        /// </summary>
        private static IResult GetMaps(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var maps = dataService.GetMaps();
            return Results.Ok(new
            {
                total = maps.Count,
                maps
            });
        }

        /// <summary>
        /// Get monsters list with pagination
        /// </summary>
        private static IResult GetMonsters(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int page = 1,
            int pageSize = 50,
            string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var (monsters, total) = dataService.GetMonsters(page, pageSize, search);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                monsters
            });
        }

        /// <summary>
        /// Get NPCs list
        /// </summary>
        private static IResult GetNpcs(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var npcs = dataService.GetNpcs();
            return Results.Ok(new
            {
                total = npcs.Count,
                npcs
            });
        }

        /// <summary>
        /// Get map regions list for NPC location selection
        /// </summary>
        private static IResult GetMapRegions(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var regions = dataService.GetMapRegions();
            return Results.Ok(new
            {
                total = regions.Count,
                regions
            });
        }

        /// <summary>
        /// Get map region detail with all points
        /// </summary>
        private static IResult GetMapRegionDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var region = dataService.GetMapRegionDetail(index);
            if (region == null)
            {
                return Results.NotFound(new { message = "区域不存在" });
            }

            return Results.Ok(region);
        }

        /// <summary>
        /// Update map region points
        /// </summary>
        private static IResult UpdateMapRegionPoints(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateRegionPointsRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMapRegionPoints(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Get magics (skills) list
        /// </summary>
        private static IResult GetMagics(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var magics = dataService.GetMagics();
            return Results.Ok(new
            {
                total = magics.Count,
                magics
            });
        }

        /// <summary>
        /// Get classes information
        /// </summary>
        private static IResult GetClasses(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var classes = dataService.GetClasses();
            return Results.Ok(new
            {
                total = classes.Count,
                classes
            });
        }

        #region Map Management

        /// <summary>
        /// Get map detail by index
        /// </summary>
        private static IResult GetMapDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var map = dataService.GetMapDetail(index);
            if (map == null)
            {
                return Results.NotFound(new { message = "地图不存在" });
            }

            return Results.Ok(map);
        }

        /// <summary>
        /// Update map by index
        /// </summary>
        private static IResult UpdateMap(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateMapRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMap(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Teleport player to map
        /// </summary>
        private static IResult TeleportPlayer(
            ClaimsPrincipal user,
            ServerDataService dataService,
            TeleportRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.CharacterName))
            {
                return Results.BadRequest(new { message = "角色名称不能为空" });
            }

            var (success, message) = dataService.TeleportPlayer(request.CharacterName, request.MapIndex, request.X, request.Y);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Clear monsters on map
        /// </summary>
        private static IResult ClearMonstersOnMap(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message, count) = dataService.ClearMonstersOnMap(index);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, count });
        }

        #endregion

        #region Monster Management

        /// <summary>
        /// Get monster detail by index
        /// </summary>
        private static IResult GetMonsterDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var monster = dataService.GetMonsterDetail(index);
            if (monster == null)
            {
                return Results.NotFound(new { message = "怪物不存在" });
            }

            return Results.Ok(monster);
        }

        /// <summary>
        /// Update monster by index
        /// </summary>
        private static IResult UpdateMonster(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateMonsterRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMonster(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Clear all instances of a monster type
        /// </summary>
        private static IResult ClearMonstersByType(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message, count) = dataService.ClearMonstersByType(index);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, count });
        }

        /// <summary>
        /// Spawn monster near player
        /// </summary>
        private static IResult SpawnMonsterNearPlayer(
            ClaimsPrincipal user,
            ServerDataService dataService,
            SpawnMonsterRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.CharacterName))
            {
                return Results.BadRequest(new { message = "角色名称不能为空" });
            }

            if (request.MonsterIndex <= 0)
            {
                return Results.BadRequest(new { message = "怪物ID无效" });
            }

            if (request.Count <= 0 || request.Count > 100)
            {
                return Results.BadRequest(new { message = "数量必须在1-100之间" });
            }

            var (success, message) = dataService.SpawnMonsterNearPlayer(
                request.CharacterName, request.MonsterIndex, request.Count, request.Range);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region NPC Management

        /// <summary>
        /// Get NPC detail by index
        /// </summary>
        private static IResult GetNpcDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var npc = dataService.GetNpcDetail(index);
            if (npc == null)
            {
                return Results.NotFound(new { message = "NPC不存在" });
            }

            return Results.Ok(npc);
        }

        /// <summary>
        /// Update NPC by index
        /// </summary>
        private static IResult UpdateNpc(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateNpcRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateNpc(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Store Management

        /// <summary>
        /// Get store items list with pagination
        /// </summary>
        private static IResult GetStoreItems(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int page = 1,
            int pageSize = 50,
            string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var (items, total) = dataService.GetStoreItems(page, pageSize, search);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                items
            });
        }

        /// <summary>
        /// Get store item detail by index
        /// </summary>
        private static IResult GetStoreItemDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var item = dataService.GetStoreItemDetail(index);
            if (item == null)
            {
                return Results.NotFound(new { message = "商城商品不存在" });
            }

            return Results.Ok(item);
        }

        /// <summary>
        /// Add new store item
        /// </summary>
        private static IResult AddStoreItem(
            ClaimsPrincipal user,
            ServerDataService dataService,
            AddStoreItemRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (request.ItemIndex <= 0)
            {
                return Results.BadRequest(new { message = "物品ID必须大于0" });
            }

            if (request.Price < 0)
            {
                return Results.BadRequest(new { message = "价格不能为负数" });
            }

            if (request.HuntGoldPrice < 0)
            {
                return Results.BadRequest(new { message = "猎金价格不能为负数" });
            }

            var (success, message, item) = dataService.AddStoreItem(request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, item });
        }

        /// <summary>
        /// Update store item
        /// </summary>
        private static IResult UpdateStoreItem(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateStoreItemRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (request.Price.HasValue && request.Price.Value < 0)
            {
                return Results.BadRequest(new { message = "价格不能为负数" });
            }

            if (request.HuntGoldPrice.HasValue && request.HuntGoldPrice.Value < 0)
            {
                return Results.BadRequest(new { message = "猎金价格不能为负数" });
            }

            var (success, message) = dataService.UpdateStoreItem(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Delete store item
        /// </summary>
        private static IResult DeleteStoreItem(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.DeleteStoreItem(index);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Magic Management

        /// <summary>
        /// Get magic detail by index
        /// </summary>
        private static IResult GetMagicDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var magic = dataService.GetMagicDetail(index);
            if (magic == null)
            {
                return Results.NotFound(new { message = "技能不存在" });
            }

            return Results.Ok(magic);
        }

        /// <summary>
        /// Update magic by index
        /// </summary>
        private static IResult UpdateMagic(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateMagicRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMagic(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Grant magic to character
        /// </summary>
        private static IResult GrantMagic(
            ClaimsPrincipal user,
            ServerDataService dataService,
            GrantMagicRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.CharacterName))
            {
                return Results.BadRequest(new { message = "角色名称不能为空" });
            }

            if (request.MagicIndex <= 0)
            {
                return Results.BadRequest(new { message = "技能ID无效" });
            }

            if (request.Level < 0)
            {
                return Results.BadRequest(new { message = "技能等级不能为负数" });
            }

            var (success, message) = dataService.GrantMagicToCharacter(
                request.CharacterName, request.MagicIndex, request.Level);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Base Stats Management

        /// <summary>
        /// Get base stats with pagination and optional class filter
        /// </summary>
        private static IResult GetBaseStats(
            ClaimsPrincipal user,
            ServerDataService dataService,
            string? classFilter = null,
            int page = 1,
            int pageSize = 50)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var (stats, total) = dataService.GetBaseStats(classFilter, page, pageSize);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                stats
            });
        }

        /// <summary>
        /// Get base stat by index
        /// </summary>
        private static IResult GetBaseStat(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var stat = dataService.GetBaseStat(index);
            if (stat == null)
            {
                return Results.NotFound(new { message = "基础属性不存在" });
            }

            return Results.Ok(stat);
        }

        /// <summary>
        /// Create new base stat
        /// </summary>
        private static IResult CreateBaseStat(
            ClaimsPrincipal user,
            ServerDataService dataService,
            CreateBaseStatRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.Class))
            {
                return Results.BadRequest(new { message = "职业不能为空" });
            }

            if (request.Level < 1)
            {
                return Results.BadRequest(new { message = "等级必须大于0" });
            }

            var (success, message, stat) = dataService.CreateBaseStat(request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, stat });
        }

        /// <summary>
        /// Update base stat by index
        /// </summary>
        private static IResult UpdateBaseStat(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateBaseStatRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateBaseStat(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Delete base stat by index
        /// </summary>
        private static IResult DeleteBaseStat(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.DeleteBaseStat(index);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Item Drops

        /// <summary>
        /// Get monsters that drop this item
        /// </summary>
        private static IResult GetItemDrops(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var drops = dataService.GetItemDrops(index);
            return Results.Ok(new
            {
                total = drops.Count,
                drops
            });
        }

        #endregion

        #region Quest Management

        /// <summary>
        /// Get quests list with pagination
        /// </summary>
        private static IResult GetQuests(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int page = 1,
            int pageSize = 50,
            string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var (quests, total) = dataService.GetQuests(page, pageSize, search);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                quests
            });
        }

        /// <summary>
        /// Get quest detail by index
        /// </summary>
        private static IResult GetQuestDetail(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var quest = dataService.GetQuestDetail(index);
            if (quest == null)
            {
                return Results.NotFound(new { message = "任务不存在" });
            }

            return Results.Ok(quest);
        }

        /// <summary>
        /// Create new quest
        /// </summary>
        private static IResult CreateQuest(
            ClaimsPrincipal user,
            ServerDataService dataService,
            AddQuestRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.QuestName))
            {
                return Results.BadRequest(new { message = "任务名称不能为空" });
            }

            var (success, message, quest) = dataService.CreateQuest(request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, quest });
        }

        /// <summary>
        /// Update quest by index
        /// </summary>
        private static IResult UpdateQuest(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            UpdateQuestRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.QuestName))
            {
                return Results.BadRequest(new { message = "任务名称不能为空" });
            }

            var (success, message) = dataService.UpdateQuest(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Delete quest by index
        /// </summary>
        private static IResult DeleteQuest(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.DeleteQuest(index);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Monster Drops Management

        /// <summary>
        /// Get drops for a monster
        /// </summary>
        private static IResult GetMonsterDrops(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var drops = dataService.GetMonsterDrops(index);
            return Results.Ok(new
            {
                total = drops.Count,
                drops
            });
        }

        /// <summary>
        /// Add drop to monster
        /// </summary>
        private static IResult AddMonsterDrop(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            AddDropRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message, drop) = dataService.AddMonsterDrop(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, drop });
        }

        /// <summary>
        /// Update monster drop
        /// </summary>
        private static IResult UpdateMonsterDrop(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            int dropId,
            UpdateDropRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMonsterDrop(index, dropId, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Delete monster drop
        /// </summary>
        private static IResult DeleteMonsterDrop(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            int dropId)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.DeleteMonsterDrop(index, dropId);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Map Respawns Management

        /// <summary>
        /// Get respawns for a map
        /// </summary>
        private static IResult GetMapRespawns(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var respawns = dataService.GetMapRespawns(index);
            return Results.Ok(new
            {
                total = respawns.Count,
                respawns
            });
        }

        /// <summary>
        /// Add respawn to map
        /// </summary>
        private static IResult AddMapRespawn(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            AddRespawnRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message, respawn) = dataService.AddMapRespawn(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, respawn });
        }

        /// <summary>
        /// Update map respawn
        /// </summary>
        private static IResult UpdateMapRespawn(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            int respawnId,
            UpdateRespawnRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMapRespawn(index, respawnId, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Delete map respawn
        /// </summary>
        private static IResult DeleteMapRespawn(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            int respawnId)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.DeleteMapRespawn(index, respawnId);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion

        #region Movement Management

        /// <summary>
        /// Get movements for a map
        /// </summary>
        private static IResult GetMapMovements(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var movements = dataService.GetMapMovements(index);
            return Results.Ok(new
            {
                total = movements.Count,
                movements
            });
        }

        /// <summary>
        /// Add movement to map
        /// </summary>
        private static IResult AddMapMovement(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            AddMovementRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message, movement) = dataService.AddMapMovement(index, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, movement });
        }

        /// <summary>
        /// Update map movement
        /// </summary>
        private static IResult UpdateMapMovement(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            int movementId,
            UpdateMovementRequest request)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.UpdateMapMovement(index, movementId, request);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        /// <summary>
        /// Delete map movement
        /// </summary>
        private static IResult DeleteMapMovement(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int index,
            int movementId)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.DeleteMapMovement(index, movementId);
            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message });
        }

        #endregion
    }
}
