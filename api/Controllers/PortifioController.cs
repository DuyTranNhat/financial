using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using api.Extension;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("api/portifio")]
    public class PortifioController : ControllerBase
    {
        private readonly IPortifolioRepository _portifolioRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IStockRepository _stockRepository;
        private readonly IFMPService _FMPService;
        public PortifioController(IPortifolioRepository portifolioRepository,
         IStockRepository stockRepository, UserManager<AppUser> userManager,
          IFMPService fMPService)
        {
            _portifolioRepository = portifolioRepository;
            _userManager = userManager;
            _stockRepository = stockRepository;
            _FMPService = fMPService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> getUserPortifio()
        {
            var userName = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(userName);
            var userPortfolio = await _portifolioRepository.GetUserPortfolio(appUser);
            return Ok(userPortfolio);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPortfolio(string symbol)
        {
            var userName = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(userName);

            var stock = await _stockRepository.getBySymbolAsync(symbol);

            if (stock == null)
            {
                stock = await _FMPService.FindStockBySymbolAsync(symbol);
                if (stock == null) {
                    return BadRequest("Stock not existed!");
                } else {
                    await _stockRepository.createAsync(stock);
                }
            }

            if (stock == null) return BadRequest("Stock not found");

            var userPortfolio = await _portifolioRepository.GetUserPortfolio(appUser);

            if (userPortfolio.Any(s => s.Symbol.ToLower() == symbol.ToLower())) 
                return BadRequest("Cannot add same stock to portfolio");

            var portfolioModel = new Portfolio
            {
                AppUserId = appUser.Id,
                StockId = stock.Id,
            };

            var portfolio = await _portifolioRepository.CreateAsync(portfolioModel);

            if (portfolio == null)
            {
                return StatusCode(500, "Cannot create");
            }
            else
            {
                return Created();
            }
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAsync(string symbol)
        {
            var userName = User.GetUsername();
            var appuser = await _userManager.FindByNameAsync(userName);
            var userPortfolio = await _portifolioRepository.GetUserPortfolio(appuser);

            var portfolioExisting = userPortfolio.FirstOrDefault(s => s.Symbol.ToLower() == symbol.ToLower());

            if (portfolioExisting != null)
            {
                await _portifolioRepository.DeleteAsync(appuser, symbol);
                return NoContent();
            }
            else
            {
                return BadRequest("Stock not in your portfolio");
            }
        }
    }
}