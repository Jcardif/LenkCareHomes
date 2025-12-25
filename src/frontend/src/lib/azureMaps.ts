/**
 * Azure Maps Search API client for address autocomplete
 * Uses the Fuzzy Search API with typeahead mode for real-time suggestions
 */

const AZURE_MAPS_SUBSCRIPTION_KEY = process.env.NEXT_PUBLIC_AZURE_MAPS_KEY || '';
const AZURE_MAPS_BASE_URL = 'https://atlas.microsoft.com';

/**
 * Address suggestion from Azure Maps
 */
export interface AddressSuggestion {
  id: string;
  address: string;
  streetAddress: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  position: {
    lat: number;
    lon: number;
  };
}

/**
 * Azure Maps Fuzzy Search response structure
 */
interface AzureMapsSearchResult {
  type: string;
  id: string;
  score: number;
  address: {
    streetNumber?: string;
    streetName?: string;
    municipalitySubdivision?: string;
    municipality?: string;
    countrySecondarySubdivision?: string;
    countrySubdivision?: string;
    countrySubdivisionName?: string;
    postalCode?: string;
    extendedPostalCode?: string;
    countryCode?: string;
    country?: string;
    countryCodeISO3?: string;
    freeformAddress?: string;
    localName?: string;
  };
  position: {
    lat: number;
    lon: number;
  };
}

interface AzureMapsSearchResponse {
  summary: {
    query: string;
    queryType: string;
    queryTime: number;
    numResults: number;
    offset: number;
    totalResults: number;
    fuzzyLevel: number;
  };
  results: AzureMapsSearchResult[];
}

/**
 * Parse Azure Maps result into a standardized AddressSuggestion
 */
function parseSearchResult(result: AzureMapsSearchResult): AddressSuggestion {
  const { address, position, id } = result;
  
  // Build street address from components
  const streetParts: string[] = [];
  if (address.streetNumber) streetParts.push(address.streetNumber);
  if (address.streetName) streetParts.push(address.streetName);
  const streetAddress = streetParts.join(' ');

  // Get city - try multiple fields
  const city = address.municipality || address.localName || address.municipalitySubdivision || '';

  // Get state code (2-letter abbreviation)
  const state = address.countrySubdivision || '';

  // Get ZIP code
  const zipCode = address.postalCode || '';

  return {
    id,
    address: address.freeformAddress || streetAddress,
    streetAddress,
    city,
    state,
    zipCode,
    country: address.countryCode || 'US',
    position: {
      lat: position.lat,
      lon: position.lon,
    },
  };
}

/**
 * Search for address suggestions using Azure Maps Fuzzy Search API
 * with typeahead mode enabled for real-time autocomplete
 * 
 * @param query - The search query (partial or full address)
 * @param options - Optional configuration
 * @returns Promise with address suggestions
 */
export async function searchAddresses(
  query: string,
  options: {
    limit?: number;
    countrySet?: string;
    lat?: number;
    lon?: number;
  } = {}
): Promise<AddressSuggestion[]> {
  if (!AZURE_MAPS_SUBSCRIPTION_KEY) {
    console.warn('Azure Maps subscription key not configured');
    return [];
  }

  if (!query || query.length < 3) {
    return [];
  }

  const { limit = 5, countrySet = 'US', lat, lon } = options;

  // Build URL with query parameters
  const params = new URLSearchParams({
    'api-version': '1.0',
    'subscription-key': AZURE_MAPS_SUBSCRIPTION_KEY,
    'query': query,
    'typeahead': 'true', // Enable predictive/autocomplete mode
    'limit': limit.toString(),
    'countrySet': countrySet,
    'language': 'en-US',
    // Filter to only addresses (not POIs)
    'idxSet': 'Addr,PAD,Str', // Address, Point Address, Street
  });

  // Add location bias if provided (improves relevance)
  if (lat !== undefined && lon !== undefined) {
    params.append('lat', lat.toString());
    params.append('lon', lon.toString());
  }

  try {
    const response = await fetch(
      `${AZURE_MAPS_BASE_URL}/search/fuzzy/json?${params.toString()}`
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error('Azure Maps API error:', response.status, errorText);
      return [];
    }

    const data: AzureMapsSearchResponse = await response.json();

    // Filter and parse results
    return data.results
      .filter((result) => {
        // Only include results with proper address information
        return (
          result.address &&
          (result.address.streetName || result.address.freeformAddress) &&
          result.address.municipality &&
          result.address.countrySubdivision
        );
      })
      .map(parseSearchResult);
  } catch (error) {
    console.error('Error searching addresses:', error);
    return [];
  }
}

/**
 * Check if Azure Maps is configured
 */
export function isAzureMapsConfigured(): boolean {
  return Boolean(AZURE_MAPS_SUBSCRIPTION_KEY);
}
