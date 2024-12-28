const subeListesi = document.querySelector('.sube-listesi');

fetch('/api/Salonlar')
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(salonlar => {
        console.log(salonlar);

        subeListesi.innerHTML = ''; // Önceki içeriği temizle

        // Sadece ilk 3 şubeyi al
        const ilkUcSube = salonlar.slice(0, 3);

        ilkUcSube.forEach(salon => {
            const subeDiv = document.createElement('div');
            subeDiv.classList.add('sube');

            // Statik resim için img elementi
            const resim = document.createElement('img');
            resim.src = `images/openaikuafor.jpeg`; // Tüm şubeler için aynı resim
            resim.alt = "Şube Resmi";
            resim.classList.add('sube-resmi');
            subeDiv.appendChild(resim);

            const h4 = document.createElement('h4');
            h4.textContent = salon.konum ? salon.konum.konumAdi : 'Belirtilmemiş Konum';

            const p2 = document.createElement('p');
            p2.textContent = `Çalışma Saatleri: ${salon.baslangicSaat ? salon.baslangicSaat.slice(0, 5) : 'Belirtilmemiş'} - ${salon.bitisSaat ? salon.bitisSaat.slice(0, 5) : 'Belirtilmemiş'}`;

            subeDiv.appendChild(h4);
            subeDiv.appendChild(p2);

            subeListesi.appendChild(subeDiv);
        });

        // Slick başlatma kodu kaldırıldı

    })
    .catch(error => {
        console.error('Şube bilgileri alınırken bir hata oluştu:', error);
        // Kullanıcıya hata mesajı gösterebilirsiniz.
    });