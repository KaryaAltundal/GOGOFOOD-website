function icerigiDegistir() {
   

    const baslik = document.getElementById("ana-baslik");
    const metin = document.getElementById("ana-metin");

     (baslik && metin) 
        baslik.innerText = "2. Hafta: Başlık değişti";
        metin.innerHTML = "İçerik değişti <strong>JavaScript</strong> .";
   
}

function tabloyuDegistir() {
    const table = document.querySelector(".dataTable tbody");
    const rows = Array.from(table.rows);

    
    const harfDegerleri = {
        'AA': 4.0,
        'BA': 3.5,
        'BB': 3.0,
        'CB': 2.5,
        'CC': 2.0,
        'DC': 1.5,
        'DD': 1.0,
        'FF': 0.0
    };

  
    rows.sort((a, b) => {
        const harfA = a.cells[4].innerText.trim(); // 4. sütun Harf notu
        const harfB = b.cells[4].innerText.trim();
        return harfDegerleri[harfB] - harfDegerleri[harfA];
    });

   
    rows.forEach(row => table.appendChild(row));
    
   
}


// javascript.js dosyanın içi şöyle olmalı:
function resmiDegistir() {
    var resim = document.getElementById("ders-resmi");
    
    if (resim.src.match("lab1_foto.png")) {
        resim.src = "/images/kedi.jpeg"; 
    } else {
        resim.src = "/images/lab1_foto.png";
    }
}

document.addEventListener('mousemove', (e) => {
    const gozBebekleri = document.querySelectorAll('.goz-bebegi');
    
    gozBebekleri.forEach(goz => {
       
        const rect = goz.getBoundingClientRect();
        const gozX = rect.left + rect.width / 2;
        const gozY = rect.top + rect.height / 2;

        
        const aci = Math.atan2(e.clientY - gozY, e.clientX - gozX);
        
        
        const mesafe = 6;
        const x = Math.cos(aci) * mesafe;
        const y = Math.sin(aci) * mesafe;

        goz.style.transform = `translate(${x}px, ${y}px)`;
    });
});