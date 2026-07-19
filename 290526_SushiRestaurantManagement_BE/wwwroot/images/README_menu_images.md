# Ảnh món ăn cho Menu

Ảnh 12 món nằm trong thư mục này (`wwwroot/images/`). Menu (`/Menu/Index`) map ảnh
theo **tên món** trong code (`ResolveImageUrl` ở `Pages/Menu/Index.cshtml`), nên chỉ cần
file có đúng tên bên dưới là hiển thị đúng. Thiếu file nào → tự hiện `placeholder.svg`.

| Món | Tên file ảnh |
|---|---|
| Salmon Sushi | `salmon-sushi.png` |
| Tuna Sushi | `tuna-sushi.png` |
| Salmon Sashimi | `salmon-sashimi.png` |
| Tuna Sashimi | `tuna-sashimi.png` |
| Ebi Nigiri | `ebi-nigiri.png` |
| California Roll | `California-roll.png` |
| Tonkotsu Ramen | `tonkotsu-ramen.png` |
| Tempura Udon | `tempura-udon.png` |
| Shrimp Tempura | `shrimp-tempura.png` |
| Matcha Latte | `match-latte.png` |
| Sakura Tea | `sakura-tea.png` |
| Mochi Ice Cream | `mochi-ice-cream.png` |

> Nếu đổi tên file, hãy sửa map `ImageMap` trong `Pages/Menu/Index.cshtml` cho khớp.
> Gợi ý: `match-latte.png` nên đổi thành `matcha-latte.png` và `California-roll.png`
> viết thường `california-roll.png` để nhất quán (nhớ sửa map + script SQL nếu đổi).
