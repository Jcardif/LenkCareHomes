'use client';

import React, { useState, useCallback, useRef, useEffect } from 'react';
import { AutoComplete, Input, Spin, Typography } from 'antd';
import { EnvironmentOutlined, SearchOutlined } from '@ant-design/icons';
import { searchAddresses, isAzureMapsConfigured, type AddressSuggestion } from '@/lib/azureMaps';

const { Text } = Typography;

export interface AddressAutocompleteProps {
  value?: string;
  onChange?: (value: string) => void;
  onAddressSelect?: (suggestion: AddressSuggestion) => void;
  placeholder?: string;
  disabled?: boolean;
  status?: 'error' | 'warning';
}

/**
 * Simple debounce function to avoid lodash dependency
 */
function debounce<T extends (...args: Parameters<T>) => void>(
  func: T,
  wait: number
): T & { cancel: () => void } {
  let timeoutId: ReturnType<typeof setTimeout> | null = null;

  const debounced = ((...args: Parameters<T>) => {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
    timeoutId = setTimeout(() => {
      func(...args);
    }, wait);
  }) as T & { cancel: () => void };

  debounced.cancel = () => {
    if (timeoutId) {
      clearTimeout(timeoutId);
      timeoutId = null;
    }
  };

  return debounced;
}

/**
 * Address autocomplete component using Azure Maps Fuzzy Search API
 * Provides real-time address suggestions as the user types
 */
export function AddressAutocomplete({
  value,
  onChange,
  onAddressSelect,
  placeholder = 'Start typing an address...',
  disabled = false,
  status,
}: AddressAutocompleteProps) {
  const [options, setOptions] = useState<{ value: string; displayText: string; label: React.ReactNode; suggestion: AddressSuggestion }[]>([]);
  const [loading, setLoading] = useState(false);
  const [isConfigured] = useState(isAzureMapsConfigured);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Debounced search function
  const debouncedSearch = useRef(
    debounce(async (searchText: string) => {
      if (!searchText || searchText.length < 3) {
        setOptions([]);
        setLoading(false);
        return;
      }

      // Cancel previous request if still pending
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      abortControllerRef.current = new AbortController();

      try {
        setLoading(true);
        const suggestions = await searchAddresses(searchText, { limit: 6 });
        
        setOptions(
          suggestions.map((suggestion, index) => {
            // Use unique ID as the value (key) for React, store display text separately
            const uniqueKey = suggestion.id || `suggestion-${index}`;
            const displayText = suggestion.streetAddress || suggestion.address;
            return {
              value: uniqueKey, // Unique value for React key
              displayText, // Store the actual address text
              suggestion,
              label: (
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 8 }}>
                  <EnvironmentOutlined style={{ color: '#5a7a6b', marginTop: 4 }} />
                  <div style={{ display: 'flex', flexDirection: 'column' }}>
                    <Text strong>{displayText}</Text>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      {suggestion.city}, {suggestion.state} {suggestion.zipCode}
                    </Text>
                  </div>
                </div>
              ),
            };
          })
        );
      } catch (error) {
        console.error('Address search error:', error);
        setOptions([]);
      } finally {
        setLoading(false);
      }
    }, 300)
  ).current;

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      debouncedSearch.cancel();
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [debouncedSearch]);

  const handleSearch = useCallback(
    (searchText: string) => {
      if (!isConfigured) return;
      setLoading(true);
      debouncedSearch(searchText);
    },
    [debouncedSearch, isConfigured]
  );

  const handleSelect = useCallback(
    (_value: string, option: { value: string; displayText: string; suggestion: AddressSuggestion }) => {
      // When an address is selected, set the input to the actual address text
      if (option.displayText) {
        onChange?.(option.displayText);
      }
      // Populate all address fields
      if (onAddressSelect && option.suggestion) {
        onAddressSelect(option.suggestion);
      }
    },
    [onAddressSelect, onChange]
  );

  const handleChange = useCallback(
    (newValue: string) => {
      onChange?.(newValue);
    },
    [onChange]
  );

  // If Azure Maps is not configured, fall back to a simple input
  if (!isConfigured) {
    return (
      <Input
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
        placeholder={placeholder}
        disabled={disabled}
        status={status}
      />
    );
  }

  return (
    <AutoComplete
      value={value}
      options={options}
      onSearch={handleSearch}
      onSelect={handleSelect}
      onChange={handleChange}
      style={{ width: '100%' }}
      disabled={disabled}
      status={status}
      notFoundContent={loading ? <Spin size="small" /> : null}
    >
      <Input
        placeholder={placeholder}
        suffix={loading ? <Spin size="small" /> : <SearchOutlined style={{ color: '#bfbfbf' }} />}
      />
    </AutoComplete>
  );
}

export default AddressAutocomplete;
