import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';
import { environment } from '../environment';

interface DnsQuery {
  countryCode: string;
  countryName: string;
  queryCount: number;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styles: [`
    .info {
      padding: 6px 8px;
      font: 14px/16px Arial, Helvetica, sans-serif;
      background: white;
      background: rgba(255,255,255,0.8);
      box-shadow: 0 0 15px rgba(0,0,0,0.2);
      border-radius: 5px;
    }
    .info h4 {
      margin: 0 0 5px;
      color: #777;
    }
    .legend {
      line-height: 18px;
      color: #555;
    }
    .legend i {
      width: 18px;
      height: 18px;
      float: left;
      margin-right: 8px;
      opacity: 0.7;
    }
  `]
})
export class AppComponent implements OnInit {
  private map!: L.Map;
  private geojsonLayer!: L.GeoJSON;
  private trafficData: Map<string, DnsQuery> = new Map();
  private info!: L.Control;

  // URL for countries GeoJSON (Natural Earth simplified)
  private readonly countriesGeoJsonUrl = 'https://raw.githubusercontent.com/datasets/geo-countries/master/data/countries.geojson';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.initMap();
    this.loadDnsTraffic();
  }

  private initMap(): void {
    this.map = L.map('mapid').setView([20, 0], 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    // Add info control
    this.info = new L.Control({ position: 'topright' });
    this.info.onAdd = () => {
      const div = L.DomUtil.create('div', 'info');
      div.innerHTML = '<h4>DNS Traffic by Country</h4>Hover over a country';
      return div;
    };
    this.info.addTo(this.map);

    // Add legend
    const legend = new L.Control({ position: 'bottomright' });
    legend.onAdd = () => {
      const div = L.DomUtil.create('div', 'info legend');
      const grades = [0, 5, 10, 20, 50, 100];
      div.innerHTML = '<h4>Queries</h4>';
      for (let i = 0; i < grades.length; i++) {
        div.innerHTML +=
          `<i style="background:${this.getColor(grades[i] + 1)}"></i> ` +
          grades[i] + (grades[i + 1] ? '&ndash;' + grades[i + 1] + '<br>' : '+');
      }
      return div;
    };
    legend.addTo(this.map);
  }

  private loadDnsTraffic(): void {
    // First, get traffic data from our API
    this.http.get<DnsQuery[]>(environment.apiUrl).subscribe({
      next: (data) => {
        // Store traffic data by country code
        data.forEach(query => {
          this.trafficData.set(query.countryCode, query);
        });
        // Then load and display the GeoJSON
        this.loadCountriesGeoJson();
      },
      error: (err) => {
        console.error('Error loading DNS traffic data:', err);
        // Load map anyway to show countries
        this.loadCountriesGeoJson();
      }
    });
  }

  private loadCountriesGeoJson(): void {
    this.http.get<GeoJSON.FeatureCollection>(this.countriesGeoJsonUrl).subscribe({
      next: (geojson) => {
        this.geojsonLayer = L.geoJSON(geojson, {
          style: (feature) => this.style(feature),
          onEachFeature: (feature, layer) => this.onEachFeature(feature, layer)
        }).addTo(this.map);
      },
      error: (err) => console.error('Error loading countries GeoJSON:', err)
    });
  }

  private style(feature: GeoJSON.Feature | undefined): L.PathOptions {
    const countryCode = feature?.properties?.['ISO_A2'] || feature?.properties?.['iso_a2'];
    const traffic = countryCode ? this.trafficData.get(countryCode) : undefined;
    const queryCount = traffic?.queryCount || 0;

    return {
      fillColor: this.getColor(queryCount),
      weight: 1,
      opacity: 1,
      color: '#666',
      fillOpacity: queryCount > 0 ? 0.7 : 0.1
    };
  }

  private getColor(count: number): string {
    return count > 100 ? '#800026' :
           count > 50  ? '#BD0026' :
           count > 20  ? '#E31A1C' :
           count > 10  ? '#FC4E2A' :
           count > 5   ? '#FD8D3C' :
           count > 0   ? '#FEB24C' :
                         '#FFEDA0';
  }

  private onEachFeature(feature: GeoJSON.Feature, layer: L.Layer): void {
    layer.on({
      mouseover: (e) => this.highlightFeature(e),
      mouseout: (e) => this.resetHighlight(e),
      click: (e) => this.zoomToFeature(e)
    });
  }

  private highlightFeature(e: L.LeafletMouseEvent): void {
    const layer = e.target as L.Path;
    layer.setStyle({
      weight: 3,
      color: '#333',
      fillOpacity: 0.9
    });
    layer.bringToFront();

    // Update info
    const feature = (layer as any).feature as GeoJSON.Feature;
    const countryCode = feature?.properties?.['ISO_A2'] || feature?.properties?.['iso_a2'];
    const countryName = feature?.properties?.['ADMIN'] || feature?.properties?.['name'] || countryCode;
    const traffic = countryCode ? this.trafficData.get(countryCode) : undefined;
    
    const infoDiv = (this.info as any)._container as HTMLElement;
    if (traffic) {
      infoDiv.innerHTML = `<h4>DNS Traffic</h4><b>${countryName}</b><br/>${traffic.queryCount} queries`;
    } else {
      infoDiv.innerHTML = `<h4>DNS Traffic</h4><b>${countryName}</b><br/>No traffic recorded`;
    }
  }

  private resetHighlight(e: L.LeafletMouseEvent): void {
    this.geojsonLayer.resetStyle(e.target);
    const infoDiv = (this.info as any)._container as HTMLElement;
    infoDiv.innerHTML = '<h4>DNS Traffic by Country</h4>Hover over a country';
  }

  private zoomToFeature(e: L.LeafletMouseEvent): void {
    this.map.fitBounds(e.target.getBounds());
  }
}
