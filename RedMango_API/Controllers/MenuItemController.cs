using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using RedMango_API.Models.Dto;
using RedMango_API.Repository;
using RedMango_API.Utility;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace RedMango_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private ApiResponse _response;

        public MenuItemController(ApplicationDbContext db, IBlobService blobService)
        {
            _db = db;
            _blobService = blobService;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItem()
        {
            _response.Result = _db.MenuItems;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
        [HttpGet("{id:int}", Name = "GetMenuItem")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            MenuItem menuItem = _db.MenuItems.FirstOrDefault(x => x.Id == id);
            if (menuItem == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);

            }
            _response.Result = menuItem;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm] MenuItemCreateDto menuItemCreateDto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemCreateDto.File == null || menuItemCreateDto.File.Length == 0)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest(_response);
                    }
                    string filename = $"{Guid.NewGuid()}{Path.GetExtension(menuItemCreateDto.File.FileName)}";
                    MenuItem menuItemToCreate = new()
                    {
                        Name = menuItemCreateDto.Name,
                        Description = menuItemCreateDto.Description,
                        Category = menuItemCreateDto.Category,
                        SpecialTag = menuItemCreateDto.SpecialTag,
                        Price = menuItemCreateDto.Price,
                        Image = await _blobService.UploadBlob(filename, SD.SD_Storage_Container, menuItemCreateDto.File)
                    };
                    _db.MenuItems.Add(menuItemToCreate);
                    _db.SaveChanges();
                    _response.Result = menuItemToCreate;
                    _response.StatusCode = HttpStatusCode.Created;
                    return CreatedAtRoute("GetMenuItem", new { id = menuItemToCreate.Id }, _response);
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessage
                    = new List<string>() { ex.Message };
            }
            return _response;
        }

        [HttpPut("{id:int}", Name = "UpdateMenuItem")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDto menuItemUpdateDto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDto == null || id != menuItemUpdateDto.Id)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }
                    MenuItem menuItemFromDb = await _db.MenuItems.FindAsync(id);
                    if (menuItemFromDb == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }
                    menuItemFromDb.Name = menuItemUpdateDto.Name;
                    menuItemFromDb.Price = menuItemUpdateDto.Price;
                    menuItemFromDb.Category = menuItemUpdateDto.Category;
                    menuItemFromDb.SpecialTag = menuItemUpdateDto.SpecialTag;
                    menuItemFromDb.Description = menuItemUpdateDto.Description;
                    if (menuItemUpdateDto.File != null && menuItemUpdateDto.File.Length > 0)
                    {
                        string filename = $"{Guid.NewGuid()}{Path.GetExtension(menuItemUpdateDto.File.FileName)}";
                        await _blobService.DeleteBlob(menuItemFromDb.Image.Split('/').Last(), SD.SD_Storage_Container);
                        menuItemFromDb.Image = await _blobService.UploadBlob(filename, SD.SD_Storage_Container, menuItemUpdateDto.File);
                    }
                    _db.MenuItems.Update(menuItemFromDb);
                    _db.SaveChanges();
                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessage
                    = new List<string>() { ex.Message };
            }
            return _response;
        }

        [HttpDelete("{id:int}", Name = "DeleteMenuItem")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }
                MenuItem menuItemFromDb = await _db.MenuItems.FindAsync(id);
                if (menuItemFromDb == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }
                await _blobService.DeleteBlob(menuItemFromDb.Image.Split('/').Last(), SD.SD_Storage_Container);

                // Add 2 second delay for deleting
                int milliseconds = 2000;
                Thread.Sleep(milliseconds);

                _db.MenuItems.Remove(menuItemFromDb);
                _db.SaveChanges();
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessage
                    = new List<string>() { ex.Message };
            }
            return _response;
        }
    }
}
