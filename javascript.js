function icerigiDegistir() {
    console.log("Butona tıklandı!"); // Çalışıp çalışmadığını konsoldan görmek için

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
    
    console.log("Tablo harf notuna göre sıralandı!");
}